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
