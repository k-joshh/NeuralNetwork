# Facial Expression Recognition Model

## Description of Project
This project aims to build and train a neural network model to identify and classify human facial expressions from image data.

---

## Description of Data
This project utilizes the **FER-2013** dataset, originally uploaded by Manas Sambare on [Kaggle](https://www.kaggle.com/datasets/msambare/fer2013). 

### Image Preprocessing & Standardization
* **Resolution & Color:** Images are flattened into a uniform grayscale format and scaled down to $48 \times 48$ pixel dimensions.
* **Alignment:** Faces are centered and normalized to occupy a consistent spatial layout within each frame.
* **Performance:** These reduced, uniform dimensions minimize computational overhead, enabling faster processing and stable feature extraction.
* **Directory Structure:** Images are organized into 7 folders corresponding to distinct facial expressions: `angry`, `disgust`, `fear`, `happy`, `sad`, `neutral`, and `surprise`.

### Subsampling Strategy
The original FER-2013 dataset contains **28,709 training images** and **3,589 test images**. 

Due to computational constraints associated with our custom logic implemented in Visual Studio, processing 30,000+ images directly was unfeasible. To address this:
* We utilized a Python script (`secret.py`) to randomly sample **30 images per emotion** from the training set.
* This generated a refined working dataset named `facial_expressions` for initial development and preprocessing.
* **Project Dataset Path:** `Dataset\facial_expressions.csv`

---

## Model Validation
Following model training on our custom subset, final validation will be conducted using the original dataset's test partition. Evaluating the model against unseen images provides a reliable measure of its generalization performance and effectiveness.

---

## References & Resources
* **Original Dataset Source:** [Kaggle - FER-2013](https://www.kaggle.com/datasets/msambare/fer2013)