Description of Project:
This project aims to train a neural network model to identify the facial expressions of a set of images.

Description of Data:
This project uses the dataset titled FER-2013 uploaded by Manas Sambare at keggle.com. The goal of the data was to learn facial experssion from the given images. The images were all flatted down to uniform greyscale, scaled down to 48x48 dimensions, and edit in a way that the face is centered or occupies the same space within each image. The smaller uniform and smaller dimensions allow for faster processing and predictability that the program can comfortably analyze and master. Each image is stored within 7 folders under the name of different expressions depending on what facial expression the subject of the image was expressing (angry, disgust, fear, happy, sad, netral, suprise).

Initially, the original dataset consisted of 28,709 images to be used for training a model while 3,589 were images to test it. My partner and I elected to use a subset of images from the training set as the main datset that will be used in this project, because of the rudimentary logic that we will be implementing in visual studio it would lead to significant computational constraints to process 30,000+ images. Using a python source code named secret.py to randomly select an image, we created a dataset called facial_expressions having 30 randomly selected image per emotion that will then be preprocessed in our code logic proper.

Orginal dataset: https://www.kaggle.com/datasets/msambare/fer2013
Project dataset: Dataset\facial_expressions.csv