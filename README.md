# Deep-Learn
import shutil
import os

# Source directories for JSON and PNG files
json_directory = 'path_to_json_directory'
png_directory = 'path_to_png_directory'

# Get a list of files in each directory
json_files = [file for file in os.listdir(json_directory) if file.endswith('.json')]
png_files = [file for file in os.listdir(png_directory) if file.endswith('.png')]

# Ensure there are pairs of files to copy
pairs = min(len(json_files), len(png_files))

# Copy files two at a time (one JSON and one PNG)
for i in range(pairs):
    json_file = os.path.join(json_directory, json_files[i])
    png_file = os.path.join(png_directory, png_files[i])

    # Copy JSON file
    shutil.copy(json_file, 'destination_directory')

    # Copy PNG file
    shutil.copy(png_file, 'destination_directory')

    print(f"Copied {json_files[i]} and {png_files[i]}")

import os
import shutil

def divide_files_into_batches(src_folder, batch_size=2000):
    files = sorted(os.listdir(src_folder))
    
    for i in range(0, len(files), batch_size):
        batch = files[i:i+batch_size]
        
        # Create a new folder for each batch
        dest_folder = os.path.join(src_folder, f'Batch_{i//batch_size + 1}')
        os.makedirs(dest_folder, exist_ok=True)
        
        # Move files to the new folder
        for file in batch:
            src_path = os.path.join(src_folder, file)
            dest_path = os.path.join(dest_folder, file)
            shutil.move(src_path, dest_path)

if __name__ == "__main__":
    source_folder = "/path/to/source/folder"
    divide_files_into_batches(source_folder)

import json

def parse_json_file(file_path):
    try:
        with open(file_path, 'r') as json_file:
            data = json.load(json_file)
        print("JSON file successfully parsed.")
        return data
    except FileNotFoundError:
        print(f"Error: File not found - {file_path}")
    except IOError as e:
        print(f"Error reading the file: {e}")
    except json.JSONDecodeError as e:
        print(f"Error parsing JSON file: {e}")
    except Exception as e:
        print(f"An unexpected error occurred: {e}")

if __name__ == "__main__":
    json_file_path = "/path/to/your/file.json"
    parsed_data = parse_json_file(json_file_path)
    
    # Use the parsed data as needed
    if parsed_data:
        # Your code here to work with the parsed data
        pass


import torch
from detectron2.config import get_cfg
from detectron2.engine import DefaultTrainer
from detectron2.data import DatasetCatalog, MetadataCatalog
from detectron2.data.transforms import Augmentation, ResizeShortestEdge, RandomFlip, RandomRotation

# Define your augmentation
augmentation = Augmentation([
    ResizeShortestEdge(short_edge_length=(640, 672, 704, 736, 768, 800), max_size=1333, sample_style="choice"),
    RandomFlip(prob=0.5, horizontal=True, vertical=False),
    RandomRotation(angle=[-30, 30], expand=False, center=None, sample_style="range"),
    # Add more augmentations as needed
])

# Set up configuration
cfg = get_cfg()

# Load your custom configuration file
cfg.merge_from_file("path/to/your/config.yaml")  # Path to your configuration file

# Load pre-trained weights
cfg.MODEL.WEIGHTS = "path/to/pretrained/weights.pth"  # Path to your pre-trained weights

# Configure training dataset
cfg.DATASETS.TRAIN = ("your_train_dataset",)

# Configure testing dataset (if applicable)
cfg.DATASETS.TEST = ()

# Number of CPU workers for data loading
cfg.DATALOADER.NUM_WORKERS = 4  # Adjust based on your system

# Number of images per batch
cfg.SOLVER.IMS_PER_BATCH = 2

# Initial learning rate
cfg.SOLVER.BASE_LR = 0.0025

# Maximum number of iterations
cfg.SOLVER.MAX_ITER = 10000

# Batch size per image for ROI heads
cfg.MODEL.ROI_HEADS.BATCH_SIZE_PER_IMAGE = 128

# Number of classes in the dataset
cfg.MODEL.ROI_HEADS.NUM_CLASSES = 80  # Change based on your dataset

# Set up metadata
metadata = MetadataCatalog.get("your_train_dataset")

# Confidence threshold for inference
cfg.MODEL.ROI_HEADS.SCORE_THRESH_TEST = 0.5

# Set up trainer with augmentation
trainer = DefaultTrainer(cfg, augmentations=[augmentation])
trainer.resume_or_load(resume=False)

# Train on multiple GPUs
if num_gpus > 1:
    trainer = torch.nn.DataParallel(trainer)

# Start training
trainer.train()


import json

def delete_annotations_by_name(coco_file_path, name_prefix):
    with open(coco_file_path, 'r') as f:
        coco_data = json.load(f)

    # Delete annotations with a name starting with the specified prefix
    coco_data['annotations'] = [annotation for annotation in coco_data['annotations'] if not annotation['name'].startswith(name_prefix)]

    # Save the modified COCO label file
    with open(coco_file_path, 'w') as f:
        json.dump(coco_data, f)

# Example usage:
coco_file_path = 'path_to_coco_label_file.json'
name_prefix_to_delete = "person_"  # Change this to the prefix of the names you want to delete

delete_annotations_by_name(coco_file_path, name_prefix_to_delete)

import json

def delete_annotations_with_prefix(coco_file_path, prefix):
    with open(coco_file_path, 'r') as f:
        coco_data = json.load(f)

    # Filter out annotations related to images with filenames starting with the specified prefix
    filtered_annotations = [annotation for annotation in coco_data['annotations'] if coco_data['images'][annotation['image_id']]['file_name'].startswith(prefix)]

    # Remove annotations with the specified prefix
    for annotation in filtered_annotations:
        image_id = annotation['image_id']
        coco_data['annotations'] = [anno for anno in coco_data['annotations'] if anno['image_id'] != image_id]
        coco_data['images'] = [image for image in coco_data['images'] if image['id'] != image_id]

    # Save the modified COCO label file
    with open(coco_file_path, 'w') as f:
        json.dump(coco_data, f)

# Example usage:
coco_file_path = 'path_to_coco_label_file.json'
prefix_to_delete = 'prefix_'  # Change this to the prefix of the filenames you want to delete annotations for

delete_annotations_with_prefix(coco_file_path, prefix_to_delete)
