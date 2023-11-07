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
