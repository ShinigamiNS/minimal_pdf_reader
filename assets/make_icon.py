import sys
import os
import io
import struct

def create_simple_ico(filename):
    # Create a 32x32 transparent image, draw a red square with some white pixels for "doc"
    # Actually just saving a raw byte structure for a 32x32 32-bit BMP wrapped in ICO
    width = 32
    height = 32
    
    # 1. ICO Header (6 bytes)
    # 00 00 (Reserved)
    # 01 00 (Type 1=ICO)
    # 01 00 (Count 1)
    
    # 2. Directory Entry (16 bytes)
    # 20 (Width 32)
    # 20 (Height 32)
    # 00 (Color count 0=>=256)
    # 00 (Reserved)
    # 01 00 (Color planes = 1)
    # 20 00 (BPP = 32)
    # bytes_in_res (4 bytes)
    # image_offset (4 bytes)
    
    # BMP Info Header (40 bytes)
    bmp_header = struct.pack('<IiiHHIIiiII',
        40, width, height * 2, 1, 32, 0, 0, 0, 0, 0, 0)
        
    pixels = bytearray()
    for y in range(height):
        for x in range(width):
            # Red background, white "text" area
            if 4 <= x <= 28 and 4 <= y <= 28:
                if 10 <= x <= 22 and 12 <= y <= 20: # pseudo text
                    pixels.extend([255, 255, 255, 255]) # BGRA 
                else:
                    pixels.extend([50, 50, 220, 255]) # BGRA (Red)
            else:
                pixels.extend([0, 0, 0, 0])
                
    AND_mask = bytearray(128) # 32 * 32 / 8 = 128 bytes, all 0 to show image
    
    img_data = bmp_header + pixels + AND_mask
    img_size = len(img_data)
    
    with open(filename, 'wb') as f:
        f.write(struct.pack('<HHH', 0, 1, 1))
        f.write(struct.pack('<BBBBHHII', width, height, 0, 0, 1, 32, img_size, 22))
        f.write(img_data)

create_simple_ico("app_icon.ico")
