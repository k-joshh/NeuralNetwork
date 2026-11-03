import pandas as pd
import os
import random

# path to our main dataset folder
dataset_path = "Dataset\\train"

# kinds of emotions in our dataset
emotions = ["angry", "disgust", "fear", "happy", "neutral", "sad","surprise"]; 

# number of images we are going to select for each emotion
num_images_per_emotion = 30

random.seed(42)  # Set a seed for reproducibility

# created a list to hold selected images per emotion
# which will then be transformed into a csv file, acting as our project's dataset
facial_expressions = []

# loop for selecting images for each emotion
for emotion in emotions:
    # path to the folder containing images for the current emotion
    emotion_folder = os.path.join(dataset_path, emotion)
    
    # get a list of all image files in the current emotion folder
    image_files = [f for f in os.listdir(emotion_folder) if os.path.isfile(os.path.join(emotion_folder, f))]
    
    # randomly select a specified number of images for the current emotion
    selected_images = random.sample(image_files, num_images_per_emotion)
    
    # add the selected images and their corresponding emotion to the facial_expressions list
    for image in selected_images:

        facial_expressions.append({"image": image, "emotion": emotion})


# turning the list of selected images per emotions into a pandas dataframe
facial_expressions_df = pd.DataFrame(facial_expressions)

# save the dataframe to a csv file
facial_expressions_df.to_csv("Dataset\\facial_expressions.csv", index=False)