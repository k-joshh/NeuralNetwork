using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Backprop;

namespace FacialExpressionNN
{
    public partial class Form1 : Form
    {
        Bitmap[] Images;
        int[] ImageEmotion;
        Bitmap[] TrainingImages;
        int[] TrainingImageEmotion;
        Bitmap[] TestingImages;
        int[] TestingImageEmotion;
        double OptimalLearningRate;
        int OptimalNumberOfEpochs, OptimalNumberOfHiddenLayers;
        Bitmap Loaded;
        NeuralNet NeuralNet;

        public Form1()
        {
            InitializeComponent();
            LoadDatasetFromCsv();
            PreProcessing();
        }

        private void LoadDatasetFromCsv()
        {
            // When running from Visual Studio (bin\Debug), Dataset is 3 levels up
            string datasetPath = GetDatasetPath();

            string csvPath = Path.Combine(datasetPath, "facial_expressions.csv");
            if (!File.Exists(csvPath))
            {
                MessageBox.Show($"CSV file not found at:\n{csvPath}", "Dataset Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] lines = File.ReadAllLines(csvPath);
            int total = lines.Length - 1; // Exclude header ("image,emotion")

            Images = new Bitmap[total];
            ImageEmotion = new int[total];

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] parts = line.Split(',');
                string fileName = parts[0].Trim();
                string emotion = parts[1].Trim();

                // Images are arranged in Dataset\train\<emotion>\<fileName>
                string imagePath = Path.Combine(datasetPath, "train", emotion, fileName);
                if (!File.Exists(imagePath))
                {
                    // Fallback if images are directly under train\
                    imagePath = Path.Combine(datasetPath, "train", fileName);
                }

                if (File.Exists(imagePath))
                {
                    // Copy to memory Bitmap to prevent GDI+ file lock
                    using (Bitmap temp = new Bitmap(imagePath))
                    {
                        Images[i - 1] = new Bitmap(temp);
                    }
                }

                ImageEmotion[i - 1] = GetEmotionIndex(emotion);
            }
        }

        private string GetDatasetPath()
        {
            // start from the folder where the executable is running
            DirectoryInfo currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            // walk up the directory tree until we find the root repository folder containing
            while (currentDir != null)
            {
                string candidatePath = Path.Combine(currentDir.FullName, "Dataset");

                if (Directory.Exists(candidatePath))
                {
                    return candidatePath;
                }

                currentDir = currentDir.Parent;
            }

            throw new DirectoryNotFoundException("Could not find the 'dataset' folder relative to the solution directory.");
        }

        private int GetEmotionIndex(string emotion)
        {

            // Convert emotion string to index 
            switch (emotion.Trim().ToLower())
            {
                case "angry": return 0;
                case "disgust": return 1;
                case "fear": return 2;
                case "happy": return 3;
                case "neutral": return 4;
                case "sad": return 5;
                case "surprise": return 6;
                default: return -1;
            }
        }

        private void PreProcessing()
        {
            // following an 80/20 split for training and testing per emotion
            int seed = 42; // fixed seed for reproducibility
            Random rand = new Random(seed);

            int totalImagesPerEmotion = 30; // total images available per emotion folder
            int trainPerEmotion = (int)(totalImagesPerEmotion * 0.8); // 24 images for training
            int testPerEmotion = totalImagesPerEmotion - trainPerEmotion; // 6 images for testing

            int totalClasses = 7;
            int trainingCount = trainPerEmotion * totalClasses; // 168 total training images
            int testingCount = testPerEmotion * totalClasses;   // 42 total testing images

            TrainingImages = new Bitmap[trainingCount];
            TrainingImageEmotion = new int[trainingCount];
            TestingImages = new Bitmap[testingCount];
            TestingImageEmotion = new int[testingCount];

            int trainIndex = 0;
            int testIndex = 0;

            // loop through each of the 7 emotion classes independently
            for (int c = 0; c < totalClasses; c++)
            {
                int classOffset = c * totalImagesPerEmotion;

                // track which local indices within this class have been picked for training
                bool[] selectedForTrain = new bool[totalImagesPerEmotion];
                int pickedCount = 0;

                // randomly select 120 unique images for training from this class
                while (pickedCount < trainPerEmotion)
                {
                    int localIdx = rand.Next(totalImagesPerEmotion);
                    if (!selectedForTrain[localIdx])
                    {
                        selectedForTrain[localIdx] = true;

                        int globalIdx = classOffset + localIdx;
                        TrainingImages[trainIndex] = Images[globalIdx];
                        TrainingImageEmotion[trainIndex] = ImageEmotion[globalIdx];
                        trainIndex++;
                        pickedCount++;
                    }
                }

                // assign the remaining 30 images from this class to the testing set
                for (int localIdx = 0; localIdx < totalImagesPerEmotion; localIdx++)
                {
                    if (!selectedForTrain[localIdx])
                    {
                        int globalIdx = classOffset + localIdx;
                        TestingImages[testIndex] = Images[globalIdx];
                        TestingImageEmotion[testIndex] = ImageEmotion[globalIdx];
                        testIndex++;
                    }
                }
            }

            // Shuffle the training and testing datasets to ensure randomness
            ShuffleDataset(TrainingImages, TrainingImageEmotion);
            ShuffleDataset(TestingImages, TestingImageEmotion);
        }

        private void ShuffleDataset(Bitmap[] images, int[] emotions)
        {
            Random rand = new Random();
            for (int i = images.Length - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                // Swap images
                Bitmap tempImage = images[i];
                images[i] = images[j];
                images[j] = tempImage;

                // Swap corresponding emotions
                int tempEmotion = emotions[i];
                emotions[i] = emotions[j];
                emotions[j] = tempEmotion;
            }
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            Loaded = new Bitmap(openFileDialog1.FileName);
            pictureBox1.Image = Loaded;
            
            int pixelIndex = 0;
            for (int x = 0; x < Loaded.Width; x++)
            {
                for (int y = 0; y < Loaded.Height; y++)
                {
                    Color pixel = Loaded.GetPixel(x, y);
                    double gray = (pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11) / 255.0;
                    NeuralNet.setInputs(pixelIndex, gray);
                    pixelIndex++;
                }
            }
            NeuralNet.run();

            int predictedClass = 0;
            double maxActivation = -1.0;
            for (int o = 0; o < 7; o++)
            {
                double activation = NeuralNet.getOuputData(o);
                if (activation > maxActivation)
                {
                    maxActivation = activation;
                    predictedClass = o;
                }
            }

            string[] emotions = { "Angry", "Disgust", "Fear", "Happy", "Neutral", "Sad", "Surprise" };
            EmotionLabel.Text = $"{emotions[predictedClass]} ({maxActivation * 100.0:F2})";
        }

        private void RandomSearchButton_Click(object sender, EventArgs e)
        {
            // ranges for random selection
            double[] LRChoices = { 0.001, 0.005, 0.01, 0.05, 0.1};
            int[] EpochChoices = { 10, 25, 50, 100 };
            int[] HiddenChoices = { 16, 32, 64, 128 };

            Random Rand = new Random();

            double BestError = double.MaxValue;

            richTextBox1.Clear();
            richTextBox1.AppendText("Starting Random Search (5 trials)...\n\n");

            // 5 trials
            for (int trial = 1; trial <= 5; trial++)
            {
                // random picking of hyperparameters
                double CurrentLR = LRChoices[Rand.Next(LRChoices.Length)];
                int CurrentEpochs = EpochChoices[Rand.Next(EpochChoices.Length)];
                int CurrentHidden = HiddenChoices[Rand.Next(HiddenChoices.Length)];

                // initialize the neural network with the selected hyperparameters
                NeuralNet = new NeuralNet(2304, CurrentHidden, 7);
                NeuralNet.setLearningRate(CurrentLR);

                double FinalTrialError = 0.0;

                // train for the selected number of epochs
                for (int epoch = 0; epoch < CurrentEpochs; epoch++)
                {
                    //loop through all images in the dataset
                    for (int idx = 0; idx < TrainingImages.Length; idx++)
                    {
                        // loop for each pixel
                        Bitmap image = TrainingImages[idx];
                        int pixelIndex = 0;
                        for (int x = 0; x < image.Width; x++)
                        {
                            for(int y = 0; y < image.Height; y++)
                            {
                                Color pixel = image.GetPixel(x,y);
                                double gray = (pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11) / 255.0;
                                NeuralNet.setInputs(pixelIndex, gray);
                                pixelIndex++;
                            }
                        }
                        for (int i = 0; i < 7; i++)
                        {
                            NeuralNet.setDesiredOutput(i, (TrainingImageEmotion[idx] == i) ? 1.0 : 0.0); // One-hot encoding for desired output
                        }
                        NeuralNet.learn(); // Train the network with the current input and desired output
                    }
                }

                // simulating an error calculation for demonstration purposes 
                FinalTrialError = CalculateError();

                // log current trial results to the richtextbox
                richTextBox1.AppendText($"Trial {trial} -> LR: {CurrentLR}, Epochs: {CurrentEpochs}, Hidden: {CurrentHidden} | Error: {FinalTrialError:F4}\n");

                // forces the UI to update so it prints live line-by-line
                Application.DoEvents();

                // track best configuration (lowest error)
                if (FinalTrialError < BestError)
                {
                    BestError = FinalTrialError;
                    OptimalLearningRate = CurrentLR;
                    OptimalNumberOfEpochs = CurrentEpochs;
                    OptimalNumberOfHiddenLayers = CurrentHidden;
                }
            }

            // display the overall winner in the richtextbox
            richTextBox1.AppendText("\n========================================\n");
            richTextBox1.AppendText($"BEST FOUND -> LR: {OptimalLearningRate}, Epochs: {OptimalNumberOfEpochs}, Hidden: {OptimalNumberOfHiddenLayers} | Min Error: {BestError:F4}\n");
            richTextBox1.AppendText("========================================\n");

            // output the best values to your corresponding UI labels
            LRTextBox.Text = $"{OptimalLearningRate}";
            EpochsTextBox.Text = $"{OptimalNumberOfEpochs}";
            HNTextBox.Text = $"{OptimalNumberOfHiddenLayers}";
        }

        private double CalculateError()
        {
            // using ArgMax method to calculate error by using test set
            if (TestingImages == null || TestingImages.Length == 0)
            {
                return 1.0; // return maximum error if test set is empty
            }

            int incorrectCount = 0;

            // loop through every image in the testing set
            for (int i = 0; i < TestingImages.Length; i++)
            {
                Bitmap img = TestingImages[i];
                int trueLabel = TestingImageEmotion[i];

                // load the 48x48 image pixels into the neural net inputs (2,304 inputs)
                int pixelIndex = 0;
                for (int x = 0; x < img.Width; x++)
                {
                    for (int y = 0; y < img.Height; y++)
                    {
                        Color pixel = img.GetPixel(x, y);
                        // Convert to grayscale and normalize between 0.0 and 1.0
                        double gray = (pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11) / 255.0;
                        NeuralNet.setInputs(pixelIndex, gray);
                        pixelIndex++;
                    }
                }

                // run the neural network to get the output activations
                NeuralNet.run();

                // use ArgMax across the 7 output neurons to find the network's prediction
                int predictedClass = 0;
                double maxActivation = -1.0;

                for (int o = 0; o < 7; o++)
                {
                    // retrieves the activation of the o-th output neuron
                    double activation = NeuralNet.getOuputData(o);
                    if (activation > maxActivation)
                    {
                        maxActivation = activation;
                        predictedClass = o;
                    }
                }

                // compare prediction to the actual emotion label
                if (predictedClass != trueLabel)
                {
                    incorrectCount++;
                }
            }

            // return the final error rate (ex. 0.15 means 15% error / 85% accuracy)
            return (double)incorrectCount / TestingImages.Length;
        }

        private double CalculateAccuracyFromArrays(Bitmap[] images, int[] emotionLabels)
        {
            if (images == null || images.Length == 0) return 0.0;

            int numCorrect = 0;
            int numTotal = images.Length;

            for (int i = 0; i < numTotal; i++)
            {
                Bitmap img = images[i];
                int trueLabel = emotionLabels[i];

                int pixelIndex = 0;
                for (int x = 0; x < img.Width; x++)
                {
                    for (int y = 0; y < img.Height; y++)
                    {
                        Color pixel = img.GetPixel(x, y);
                        double gray = (pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11) / 255.0;
                        NeuralNet.setInputs(pixelIndex, gray);
                        pixelIndex++;
                    }
                }

                NeuralNet.run();

                int predictedClass = 0;
                double maxActivation = -1.0;

                for (int o = 0; o < 7; o++)
                {
                    double activation = NeuralNet.getOuputData(o);
                    if (activation > maxActivation)
                    {
                        maxActivation = activation;
                        predictedClass = o;
                    }
                }

                if (predictedClass == trueLabel)
                {
                    numCorrect++;
                }
            }

            return (double)numCorrect / numTotal;
        }

        private void TrainButton_Click(object sender, EventArgs e)
        {
            // this is the training data process
            richTextBox1.AppendText("\n---------------------------------\n");
            richTextBox1.AppendText("\nTraining the neural network...\n");

            NeuralNet = new NeuralNet(2304, int.Parse(HNTextBox.Text), 7);
            NeuralNet.setLearningRate(double.Parse(LRTextBox.Text));

            richTextBox1.AppendText("Starting training\n");

            // loop through your total epochs (e.g., bestEpochs found by random search)
            for (int epoch = 1; epoch <= int.Parse(EpochsTextBox.Text); epoch++)
            {
                for (int idx = 0; idx < TrainingImages.Length; idx++)
                {
                    Bitmap image = TrainingImages[idx];
                    
                    int pixelIndex = 0;
                    for (int x = 0; x < image.Width; x++)
                    {
                        for (int y = 0; y < image.Height; y++)
                        {
                            Color pixel = image.GetPixel(x, y);
                            double gray = (pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11) / 255.0;
                            NeuralNet.setInputs(pixelIndex, gray);
                            pixelIndex++;
                        }
                    }

                    for (int i = 0; i < 7; i++)
                    {
                        NeuralNet.setDesiredOutput(i, (TrainingImageEmotion[idx] == i) ? 1.0 : 0.0);
                    }

                    NeuralNet.learn();
                }

                if (epoch % 100 == 0)
                {
                    double currentAcc = CalculateAccuracyFromArrays(TrainingImages, TrainingImageEmotion);
                    double estimatedError = 1.0 - currentAcc;

                    richTextBox1.AppendText($"epoch = {epoch,-4}    error = {estimatedError:F4}\n");
                    
                    Application.DoEvents(); 
                }
            }

            richTextBox1.AppendText("Done\n");

            double trainAcc = CalculateAccuracyFromArrays(TrainingImages, TrainingImageEmotion);
            double testAcc  = CalculateAccuracyFromArrays(TestingImages, TestingImageEmotion);

            richTextBox1.AppendText("---------------------------------------------\n");
            richTextBox1.AppendText($"Training Accuracy: {trainAcc * 100.0:F2}%\n");
            richTextBox1.AppendText($"Testing Set Accuracy (80/20): {testAcc * 100.0:F2}%\n");
            richTextBox1.AppendText("---------------------------------------------\n");

        }

        private void LoadImageButton_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }
    }
}
