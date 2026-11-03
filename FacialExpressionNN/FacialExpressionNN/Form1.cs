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
        double LearningRate;
        int NumberOfEpochs, NumberOfHiddenLayers;
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
            string datasetPath = "C:\\Users\\Khylle\\Documents\\BSCS\\3rd Year\\CS345\\NeuralNetwork\\Dataset";

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
        }

        private void RandomSearchButton_Click(object sender, EventArgs e)
        {
            // ranges for random selection
            double[] LRChoices = { 0.001, 0.005, 0.01, 0.05, 0.1, 0.2 };
            int[] EpochChoices = { 10, 25, 50, 100 };
            int[] HiddenChoices = { 16, 32, 64, 128 };

            Random Rand = new Random();

            double BestError = double.MaxValue;
            double BestLR = 0;
            int BestEpochs = 0;
            int BestHidden = 0;

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
                        for(int x = 0; x < image.Width; x++)
                        {
                            for(int y = 0; y < image.Height; y++)
                            {
                                Color pixel = image.GetPixel(x,y);
                                int grey = (pixel.R + pixel.G + pixel.B) / 3;
                                NeuralNet.setInputs(x * image.Height + y, grey / 255.0);
                            }
                        }
                        for (int i = 0; i < 7; i++)
                        {
                            NeuralNet.setDesiredOutput(i, (TrainingImageEmotion[idx] == i) ? 1.0 : 0.0); // One-hot encoding for desired output
                        }
                        NeuralNet.learn(); // Train the network with the current input and desired output
                    }
                }

                // Simulating an error calculation for demonstration purposes 
                FinalTrialError = CalculateError();

                // log current trial results to the richtextbox
                richTextBox1.AppendText($"Trial {trial} -> LR: {CurrentLR}, Epochs: {CurrentEpochs}, Hidden: {CurrentHidden} | Error: {FinalTrialError:F4}\n");

                // forces the UI to update so it prints live line-by-line
                Application.DoEvents();

                // track best configuration (lowest error)
                if (FinalTrialError < BestError)
                {
                    BestError = FinalTrialError;
                    BestLR = CurrentLR;
                    BestEpochs = CurrentEpochs;
                    BestHidden = CurrentHidden;
                }
            }

            // display the overall winner in the richtextbox
            richTextBox1.AppendText("\n========================================\n");
            richTextBox1.AppendText($"BEST FOUND -> LR: {BestLR}, Epochs: {BestEpochs}, Hidden: {BestHidden} | Min Error: {BestError:F4}\n");
            richTextBox1.AppendText("========================================\n");

            // output the best values to your corresponding UI labels
            LearningRateLabel.Text = $"Best LR: {BestLR}";
            EpochsLabel.Text = $"Best Epochs: {BestEpochs}";
            HiddenNeuronsLabel.Text = $"Best Hidden: {BestHidden}";
        }

        private double CalculateError()
        {
            // using ArgMax method to calculate error
            if (TestingImages == null || TestingImages.Length == 0)
            {
                return 1.0; // Return maximum error if test set is empty
            }

            int incorrectCount = 0;

            // Loop through every image in the testing set
            for (int i = 0; i < TestingImages.Length; i++)
            {
                Bitmap img = TestingImages[i];
                int trueLabel = TestingImageEmotion[i];

                // 1. Flatten and load the 48x48 image pixels into the neural net inputs (2,304 inputs)
                int pixelIndex = 0;
                for (int y = 0; y < img.Height; y++)
                {
                    for (int x = 0; x < img.Width; x++)
                    {
                        Color pixel = img.GetPixel(x, y);
                        // Convert to grayscale and normalize between 0.0 and 1.0
                        double gray = (pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11) / 255.0;
                        NeuralNet.setInputs(pixelIndex, gray);
                        pixelIndex++;
                    }
                }

                // Note: If your NeuralNet class requires an explicit forward pass method 
                // after setting inputs, call it here (e.g., NeuralNet.RunForward();)

                // 2. Use ArgMax across the 7 output neurons to find the network's prediction
                int predictedClass = 0;
                double maxActivation = -1.0;

                for (int o = 0; o < 7; o++)
                {
                    // Retrieves the activation of the o-th output neuron
                    double activation = NeuralNet.getOuputData(o);
                    if (activation > maxActivation)
                    {
                        maxActivation = activation;
                        predictedClass = o;
                    }
                }

                // 3. Compare prediction to the actual emotion label
                if (predictedClass != trueLabel)
                {
                    incorrectCount++;
                }
            }

            // Return the final error rate (e.g., 0.15 means 15% error / 85% accuracy)
            return (double)incorrectCount / TestingImages.Length;
        }

        private void TrainButton_Click(object sender, EventArgs e)
        {
            
        }

        private void LoadImageButton_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }
    }
}
