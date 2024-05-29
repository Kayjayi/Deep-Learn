import cv2
import torch
from detectron2.engine import DefaultPredictor
from detectron2.config import get_cfg
from detectron2.structures import Instances
from detectron2.utils.visualizer import Visualizer
from torch.cuda import Stream

def configure_model(config_path, weights_path):
    cfg = get_cfg()
    cfg.merge_from_file(config_path)
    cfg.MODEL.WEIGHTS = weights_path
    cfg.MODEL.DEVICE = "cuda"  # Ensure the model runs on GPU
    return DefaultPredictor(cfg)

def tile_image(image, tile_size, overlap):
    height, width, _ = image.shape
    tiles = []
    for y in range(0, height, tile_size - overlap):
        for x in range(0, width, tile_size - overlap):
            tile = image[y:y+tile_size, x:x+tile_size]
            tiles.append((x, y, tile))
    return tiles

def merge_predictions(image, tiles, predictions, tile_size, overlap):
    height, width, _ = image.shape
    final_result = Instances(image_size=(height, width))
    all_boxes = []
    all_scores = []
    all_classes = []
    for (x, y, tile), pred in zip(tiles, predictions):
        pred_boxes = pred.pred_boxes.tensor + torch.tensor([x, y, x, y], device=pred.pred_boxes.tensor.device)
        all_boxes.append(pred_boxes)
        all_scores.append(pred.scores)
        all_classes.append(pred.pred_classes)

    final_result.pred_boxes = torch.cat(all_boxes).cpu()
    final_result.scores = torch.cat(all_scores).cpu()
    final_result.pred_classes = torch.cat(all_classes).cpu()
    return final_result

def process_tiles(predictor, batch_tiles, tile_size, stream):
    batch_images = torch.cat([torch.as_tensor(tile[2], device="cuda").permute(2, 0, 1).unsqueeze(0).float() for tile in batch_tiles])
    with torch.cuda.stream(stream):
        batch_predictions = predictor.model(batch_images)
    
    batch_instances = [Instances(image_size=(tile_size, tile_size)).to("cpu") for _ in batch_predictions]
    for inst, pred in zip(batch_instances, batch_predictions):
        inst.pred_boxes = pred["instances"].pred_boxes.to("cpu")
        inst.scores = pred["instances"].scores.to("cpu")
        inst.pred_classes = pred["instances"].pred_classes.to("cpu")
    
    return batch_instances

def run_single_image_inference(image, predictor, tile_size=800, overlap=100, batch_size=4):
    tiles = tile_image(image, tile_size, overlap)
    predictions = []
    stream = Stream()  # Create a CUDA stream for asynchronous processing

    for i in range(0, len(tiles), batch_size):
        batch_tiles = tiles[i:i+batch_size]
        batch_predictions = process_tiles(predictor, batch_tiles, tile_size, stream)
        predictions.extend(batch_predictions)
    
    torch.cuda.synchronize()  # Wait for all CUDA operations to finish
    final_result = merge_predictions(image, tiles, predictions, tile_size, overlap)
    return final_result

def visualize_and_display_results(image, results):
    v = Visualizer(image[:, :, ::-1], scale=1.0)
    out = v.draw_instance_predictions(results)
    result_image = out.get_image()[:, :, ::-1]
    cv2.imshow('Real-Time Inference', result_image)
    cv2.waitKey(0)  # Wait for key press to close the window

def real_time_inference(image_path, config_path, weights_path, tile_size=800, overlap=100, batch_size=4):
    image = cv2.imread(image_path)
    predictor = configure_model(config_path, weights_path)
    
    final_result = run_single_image_inference(image, predictor, tile_size, overlap, batch_size)
    visualize_and_display_results(image, final_result)

# Example usage for real-time inference on a single image
image_path = "path/to/large_image.jpg"
config_path = "path/to/config/file.yaml"
weights_path = "path/to/weights.pth"

real_time_inference(image_path, config_path, weights_path)
