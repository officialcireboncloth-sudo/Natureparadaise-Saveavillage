"""Gameplay extension for the four reviewed G10 flowers."""
from crop_extension_config import *
NEW=[('G10','Sunflower','sunflower','Bunga Matahari'),('G10','Chrysanthemum','chrysanthemum','Krisan'),('G10','Dahlia','dahlia','Dahlia'),('G10','Marigold','marigold','Marigold')]
FLOWERS=FLOWERS|{r[2] for r in NEW}
BASELINE=OUT/'G10_Baseline.json'
