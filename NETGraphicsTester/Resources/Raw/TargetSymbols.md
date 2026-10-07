.logIn support of this [issue](https://devtopia.esri.com/runtime/home-one/issues/4557). Will create a JSON for each symbol type that can be used for testing and to verify min ship goals are met. 

Simple Symbology API:
- [x] PictureFillSymbol [PictureFillSymbol.json](https://devtopia.esri.com/user-attachments/files/73719/PictureFillSymbol.json)

- [x] <details>
  <summary>SimpleFillSymbol</summary>
  
  From API -> JSON
  ```
   {
        "color": [230, 152, 0, 255],
        "style": "esriSFSSolid",
        "type": "esriSFS"
   }

  ```
  
  From style file
  ```
  {
    "type": "CIMPolygonSymbol",
    "symbolLayers": [
        {
        "type": "CIMSolidFill",
        "enable": true,
        "name": "New_53fef342-0925-4c13-93b0-b466a9e6d78b",
        "color": [230, 152, 0, 255]
        }
    ],
    "angleAlignment": "Map"
  }
  ```
  </details>
- [x] <details>
  <summary>SimpleLineSymbol</summary>

  From API -> JSON
  
  ```
    {
        "color": [0, 0, 0, 255],
        "style": "esriSLSLongDash",
        "type": "esriSLS",
        "width": 2.25
    }
  ```

  From style file
  ```
    {
        "type": "CIMLineSymbol",
        "effects": [
            { "type": "CIMGeometricEffectAddControlPoints", "angleTolerance": 120 }
        ],
        "symbolLayers": [
            {
            "type": "CIMSolidStroke",
            "effects": [
                {
                    "type": "CIMGeometricEffectDashes",
                    "dashTemplate": [5, 5],
                    "lineDashEnding": "HalfPattern",
                    "controlPointEnding": "HalfPattern"
                }
            ],
            "enable": true,
            "capStyle": "Butt",
            "joinStyle": "Round",
            "lineStyle3D": "Strip",
            "miterLimit": 10,
            "width": 100,
            "height3D": 1,
            "anchor3D": "Center",
            "color": [0, 0, 0, 255]
            }
        ]
    }

  ```
  </details>
- [x] <details>
  <summary>PictureMarkerSymbol</summary>
  
  ```
    {
    "haloSize": 1,
    "scaleX": 1,
    "symbolLayers": [
        {
        "URL": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAZAAAAGQCAMAAAC3Ycb+AAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJ\nbWFnZVJlYWR5ccllPAAAAu5QTFRFAAAAPT8lPzEZWkwnZXU0dW4tPT8lPzEZWVcpWkwnY1shZXU0\ndW4tPT8lPzEZUE8oWkwnYl4qZXU0dW4tPT8lPzEZS0snUUYdWkwnY1shZXU0Z2IrdW4tPT8lRjkb\nSEgnWkwnZXU0amUrdW4tnI0lPT8lPzEZRkcmUE8oWkwnYl4qZXU0dW4tPT8lPzEZRUYmTUwnWkwn\nY1shZXU0dW4tPT8lREUmS0snUlEoWVcpWkwnYFwqY1shZXU0Z2IrcmosdW4tPT8lSUknWkwnXFkp\nY1omZXU0ZlsqdW4tPT8lPzEZQ0QmTk0nU1IoWkwnXU8oX1sqY1shZVopZXU0cWordW4tPT8lPzEZ\nQkMmR0gmVlQpWkwnXE8oZXU0dW4tPT8lPzEZQkMmS0snUE8oWVcpWkwnYVUpZV0iZXU0dWcgdW4t\nPT8lPzIaQUMmRkYmSkonWkwnYlYpZGMnZXU0cmsrc2stdG0sdW4tPT8lPzEZQUImRUYmSUknUVAo\nVVMoWkwnY1shZXExZXU0aWQrbWcsdW4tPT8lPzEZQUImREUmSEgnUkMbWkwnY1shZXU0amUrcGcs\ndW4tnI0lPT0kPT8lPjwiPzEZQTQaQUImQjUaREUmRTcaRjkbRjkcRkcmSDwbSEgnSUgmSkklSz0b\nS0snT0MdT04oUEIbUEQfUUYdUU4kUlEoU0keVkseVlIjVlQpV0seV0wiV1QmWVcpWkwnWlEfW1Uj\nXE4dXE4nXFMgXVkqX1kiYFYgYFslYFwqYVUpYVcmYVghYlQeY1shY10iZFYeZFkpZFsnZFwiZF8r\nZVgfZV0jZV8mZWAqZXU0Zl8jZ2IraFkeaF0qaF8naF8oaGAkaWElamIma2Era2Mma2Mpa2UsbGUn\nbGYrbHgybWYobl8fbmYsbmcpbmgsbmsnb2gpb2gqcWkqcmUhcmorcmoscmstcnEpc2Ugc2wsc2wt\ndG0sdW4teWsheWwhf3AhhXYijoQmkIIklocknI0l6mLzxgAAAJN0Uk5TABAQEBAQICAgICAgIDAw\nMDAwMDBAQEBAQEBAQEBQUFBQUFBQUGBgYGBgYGBgcHBwcHBwcHCAgICAgICAgICAgICPj4+Pj4+P\n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",
        "anchorPoint": { "x": 0, "y": 0 },
        "billboardMode3D": "FaceNearPlane",
        "colorLocked": false,
        "depth3D": 0.0,
        "dominantSizeAxis3D": "Y",
        "enable": true,
        "invertBackfaceTexture": true,
        "scaleX": 1.0,
        "size": 50.0,
        "textureFilter": "Draft",
        "tintColor": [0, 0, 0, 0],
        "type": "CIMPictureMarker",
        "verticalOrientation3D": false
        }
    ],
    "type": "CIMPointSymbol"
    }

  ```
  </details>
- [x] <details>
  <summary>SimpleMarkerSymbol</summary>
  From API -> JSON

  ```
    {
        "angle": 0.0,
        "color": [255, 0, 0, 255],
        "size": 7.5,
        "style": "esriSMSCircle",
        "type": "esriSMS",
        "xoffset": 0.0,
        "yoffset": 0.0
    }
  ```

  From style file
  ```
    {
    "type": "CIMPointSymbol",
    "symbolLayers": [
        {
        "type": "CIMVectorMarker",
        "enable": true,
        "name": "New_7f99ab7b-c9b6-4954-b23f-3f4b9c7d2fb8",
        "anchorPointUnits": "Relative",
        "dominantSizeAxis3D": "Y",
        "size": 10,
        "billboardMode3D": "FaceNearPlane",
        "frame": { "xmin": -5.0, "ymin": -5.0, "xmax": 5.0, "ymax": 5.0 },
        "markerGraphics": [
            {
            "type": "CIMMarkerGraphic",
            "geometry": {
                "rings": [
                [
                    [0.0, 5.0],
                    [0.87, 4.92],
                    [1.71, 4.7],
                    [2.5, 4.33],
                    [3.21, 3.83],
                    [3.83, 3.21],
                    [4.33, 2.5],
                    [4.7, 1.71],
                    [4.92, 0.87],
                    [5.0, 0.0],
                    [4.92, -0.87],
                    [4.7, -1.71],
                    [4.33, -2.5],
                    [3.83, -3.21],
                    [3.21, -3.83],
                    [2.5, -4.33],
                    [1.71, -4.7],
                    [0.87, -4.92],
                    [0.0, -5.0],
                    [-0.87, -4.92],
                    [-1.71, -4.7],
                    [-2.5, -4.33],
                    [-3.21, -3.83],
                    [-3.83, -3.21],
                    [-4.33, -2.5],
                    [-4.7, -1.71],
                    [-4.92, -0.87],
                    [-5.0, -0.0],
                    [-4.92, 0.87],
                    [-4.7, 1.71],
                    [-4.33, 2.5],
                    [-3.83, 3.21],
                    [-3.21, 3.83],
                    [-2.5, 4.33],
                    [-1.71, 4.7],
                    [-0.87, 4.92],
                    [0.0, 5.0]
                ]
                ]
            },
            "symbol": {
                "type": "CIMPolygonSymbol",
                "symbolLayers": [
                {
                    "type": "CIMSolidFill",
                    "enable": true,
                    "name": "New_d1d52b9d-36df-4aaf-bf57-5248b4a57fb4",
                    "color": [169, 0, 230, 255]
                }
                ],
                "angleAlignment": "Map"
            }
            }
        ],
        "scaleSymbolsProportionally": true,
        "respectFrame": true
        }
    ],
    "haloSize": 1,
    "scaleX": 1,
    "angleAlignment": "Display"
    }

  ```
  </details>
- [x] <details>
  <summary>SimpleTextSymbol</summary>
  From API -> JSON

  ```
    {
        "angle": 0.0,
        "backgroundColor": [0, 0, 0, 0],
        "borderLineColor": [0, 0, 0, 0],
        "borderLineSize": 0.0,
        "color": [0, 0, 0, 255],
        "font": {
            "decoration": "none",
            "size": 18.0,
            "style": "normal",
            "weight": "normal"
        },
        "haloColor": [0, 0, 0, 0],
        "haloSize": 0.0,
        "horizontalAlignment": "center",
        "kerning": false,
        "text": "abc",
        "type": "esriTS",
        "verticalAlignment": "middle",
        "xoffset": 0.0,
        "yoffset": 0.0
    }

  ```

  From style file
  ```
    {
    "type": "CIMPointSymbol",
    "symbolLayers": [
        {
        "type": "CIMVectorMarker",
        "enable": true,
        "anchorPointUnits": "Relative",
        "dominantSizeAxis3D": "Y",
        "size": 20,
        "billboardMode3D": "FaceNearPlane",
        "frame": { "xmin": -5.0, "ymin": -5.0, "xmax": 5.0, "ymax": 5.0 },
        "markerGraphics": [
            {
            "type": "CIMMarkerGraphic",
            "geometry": { "x": 0, "y": 0 },
            "symbol": {
                "type": "CIMTextSymbol",
                "blockProgression": "TTB",
                "depth3D": 1,
                "extrapolateBaselines": true,
                "fontEffects": "Normal",
                "fontEncoding": "Unicode",
                "fontFamilyName": "Tahoma",
                "fontStyleName": "Regular",
                "fontType": "Unspecified",
                "haloSize": 1,
                "height": 8,
                "hinting": "Default",
                "horizontalAlignment": "Center",
                "kerning": true,
                "letterWidth": 100,
                "ligatures": true,
                "lineGapType": "ExtraLeading",
                "symbol": {
                "type": "CIMPolygonSymbol",
                "symbolLayers": [
                    {
                    "type": "CIMSolidFill",
                    "enable": true,
                    "color": [51, 51, 51, 255]
                    }
                ],
                "angleAlignment": "Map"
                },
                "textCase": "Normal",
                "textDirection": "LTR",
                "verticalAlignment": "Center",
                "verticalGlyphOrientation": "Right",
                "wordSpacing": 100,
                "billboardMode3D": "FaceNearPlane"
            },
            "textString": "abc"
            }
        ],
        "scaleSymbolsProportionally": true,
        "respectFrame": true
        }
    ],
    "haloSize": 1,
    "scaleX": 1,
    "angleAlignment": "Display"
    }

  ```
  </details>
- [ ] <details>
  <summary>ModelSceneSymbol</summary>
  
  ```
    Neither the `fromJSON` or `toJSON` methods are implemented for this class. 
  ```
  </details>
- [ ] <details>
  <summary>SimpleMarkerSceneSymbol</summary>
  
  ```
    Neither the `fromJSON` or `toJSON` methods are implemented for this class. 
  ```
  </details>

Esri WebStyle Symbols
- [X] 2D WebStyles
<details>
  <summary>1. Pin</summary>
  
  ```
    {
    "type": "CIMPointSymbol",
    "symbolLayers": [
        {
        "type": "CIMVectorMarker",
        "enable": true,
        "colorLocked": true,
        "anchorPoint": {
            "x": 0,
            "y": -7,
            "z": 0
        },
        "anchorPointUnits": "Absolute",
        "dominantSizeAxis3D": "Y",
        "size": 4,
        "billboardMode3D": "FaceNearPlane",
        "frame": {
            "xmin": 0,
            "ymin": 0,
            "xmax": 84.3,
            "ymax": 84.3
        },
        "markerGraphics": [
            {
            "type": "CIMMarkerGraphic",
            "geometry": {
                "rings": [
                [
                    [83.2, 32.5],
                    [84.3, 40.7],
                    [83.8, 48.9],
                    [81.7, 56.9],
                    [78.1, 64.3],
                    [73, 70.9],
                    [66.9, 76.4],
                    [59.7, 80.5],
                    [51.9, 83.2],
                    [43.7, 84.3],
                    [35.4, 83.8],
                    [27.4, 81.7],
                    [20, 78],
                    [13.4, 73],
                    [7.9, 66.8],
                    [3.8, 59.7],
                    [1.1, 51.9],
                    [0, 43.7],
                    [0.5, 35.4],
                    [2.6, 27.4],
                    [6.3, 20],
                    [11.3, 13.4],
                    [17.5, 7.9],
                    [24.7, 3.8],
                    [32.5, 1.1],
                    [39.8, 0.1],
                    [47.1, 0.3],
                    [54.3, 1.8],
                    [61.1, 4.5],
                    [67.4, 8.4],
                    [72.9, 13.3],
                    [77.4, 19.1],
                    [80.9, 25.5],
                    [83.2, 32.5]
                ]
                ]
            },
            "symbol": {
                "type": "CIMPolygonSymbol",
                "symbolLayers": [
                {
                    "type": "CIMSolidFill",
                    "enable": true,
                    "color": [242, 242, 242, 255]
                }
                ]
            }
            }
        ],
        "scaleSymbolsProportionally": true,
        "respectFrame": true,
        "clippingPath": {
            "type": "CIMClippingPath",
            "clippingType": "Intersect",
            "path": {
            "rings": [
                [
                [0, 0],
                [84.3, 0],
                [84.3, 84.3],
                [0, 84.3],
                [0, 0]
                ]
            ]
            }
        }
        },
        {
        "type": "CIMVectorMarker",
        "enable": true,
        "anchorPoint": {
            "x": 0,
            "y": -5,
            "z": 0
        },
        "anchorPointUnits": "Absolute",
        "dominantSizeAxis3D": "Y",
        "size": 10,
        "billboardMode3D": "FaceNearPlane",
        "frame": {
            "xmin": 0,
            "ymin": 0,
            "xmax": 197.7,
            "ymax": 294.7
        },
        "markerGraphics": [
            {
            "type": "CIMMarkerGraphic",
            "geometry": {
                "rings": [
                [
                    [98.9, 0],
                    [119.4, 23.2],
                    [139.4, 49.3],
                    [156.8, 75.2],
                    [171.2, 100.8],
                    [182.4, 125.3],
                    [190.6, 148.8],
                    [195.7, 171.4],
                    [197.7, 192.9],
                    [197.7, 195.8],
                    [197.7, 200.3],
                    [197.6, 202.5],
                    [197.5, 204.8],
                    [197.3, 207.1],
                    [197, 209.4],
                    [196.7, 211.7],
                    [196.4, 214.1],
                    [196, 216.4],
                    [195.5, 218.7],
                    [195, 221.1],
                    [194.4, 223.4],
                    [193.7, 225.8],
                    [193, 228.1],
                    [192.2, 230.5],
                    [191.4, 232.8],
                    [190.5, 235.1],
                    [189.5, 237.5],
                    [188.5, 239.7],
                    [187.4, 242],
                    [186.2, 244.3],
                    [185, 246.5],
                    [183.7, 248.7],
                    [182.4, 250.9],
                    [181, 253.1],
                    [179.5, 255.2],
                    [178, 257.3],
                    [176.4, 259.4],
                    [174.7, 261.4],
                    [173.1, 263.3],
                    [171.3, 265.3],
                    [169.5, 267.2],
                    [167.7, 269],
                    [165.8, 270.8],
                    [163.9, 272.5],
                    [161.9, 274.2],
                    [159.9, 275.8],
                    [157.8, 277.4],
                    [155.7, 278.9],
                    [153.6, 280.4],
                    [151.4, 281.7],
                    [149.2, 283.1],
                    [147, 284.4],
                    [144.8, 285.6],
                    [142.5, 286.7],
                    [140.3, 287.8],
                    [138, 288.8],
                    [135.7, 289.8],
                    [133.4, 290.7],
                    [131, 291.5],
                    [128.7, 292.3],
                    [126.4, 293],
                    [124, 293.6],
                    [121.7, 294.2],
                    [119.4, 294.7],
                    [117, 295.2],
                    [114.7, 295.6],
                    [112.4, 296],
                    [110.1, 296.3],
                    [107.8, 296.5],
                    [105.5, 296.7],
                    [103.3, 296.8],
                    [101.1, 296.9],
                    [98.8, 296.9],
                    [83.1, 295.7],
                    [67.8, 292],
                    [53.3, 285.9],
                    [39.9, 277.5],
                    [28.1, 267.2],
                    [18, 255.1],
                    [10, 241.5],
                    [4.2, 226.9],
                    [0.9, 211.5],
                    [0, 195.8],
                    [0.1, 192.9],
                    [2.1, 171.4],
                    [7.2, 148.8],
                    [15.4, 125.3],
                    [26.6, 100.8],
                    [41, 75.2],
                    [58.4, 49.3],
                    [78.4, 23.2],
                    [98.9, 0]
                ]
                ]
            },
            "symbol": {
                "type": "CIMPolygonSymbol",
                "symbolLayers": [
                {
                    "type": "CIMSolidFill",
                    "enable": true,
                    "color": [17, 26, 40, 255]
                }
                ]
            }
            }
        ],
        "scaleSymbolsProportionally": true,
        "respectFrame": true,
        "clippingPath": {
            "type": "CIMClippingPath",
            "clippingType": "Intersect",
            "path": {
            "rings": [
                [
                [0, 0],
                [197.7, 0],
                [197.7, 294.7],
                [0, 294.7],
                [0, 0]
                ]
            ]
            }
        }
        }
    ],
    "haloSize": 1,
    "scaleX": 1,
    "angleAlignment": "Display"
    }
  ```
</details>
<details>
  <summary>2. Boat </summary>
  
  ```
    {
    "type": "CIMPointSymbol",
    "symbolLayers": [
        {
        "type": "CIMVectorMarker",
        "enable": true,
        "name": "Group 5",
        "colorLocked": true,
        "anchorPointUnits": "Relative",
        "dominantSizeAxis3D": "Y",
        "size": 18.75,
        "billboardMode3D": "FaceNearPlane",
        "frame": {
            "xmin": 0,
            "ymin": 0,
            "xmax": 21,
            "ymax": 21
        },
        "markerGraphics": [
            {
            "type": "CIMMarkerGraphic",
            "geometry": {
                "rings": [
                [
                    [12, 17],
                    [12, 19],
                    [11, 19],
                    [11, 17],
                    [12, 17]
                ],
                [
                    [15.94, 17.43],
                    [15.42, 17.95],
                    [14.05, 16.58],
                    [14.57, 16.05],
                    [15.94, 17.43]
                ],
                [
                    [8.95, 16.58],
                    [7.58, 17.95],
                    [7.06, 17.43],
                    [8.43, 16.05],
                    [8.95, 16.58]
                ],
                [
                    [10, 11],
                    [13.48, 11],
                    [13.88, 9],
                    [10, 9],
                    [10, 11]
                ],
                [
                    [12, 3],
                    [13.28, 3.08],
                    [14.45, 3.33],
                    [15.52, 3.73],
                    [16.48, 4.3],
                    [17.34, 5.03],
                    [18.06, 5.91],
                    [18.63, 6.94],
                    [19, 8],
                    [15.16, 7.68],
                    [14.5, 11],
                    [16, 11],
                    [16, 12],
                    [12, 12],
                    [12, 14],
                    [13, 14],
                    [13, 14.92],
                    [13, 15.01],
                    [12.97, 15.19],
                    [12.91, 15.36],
                    [12.83, 15.51],
                    [12.72, 15.65],
                    [12.59, 15.77],
                    [12.44, 15.87],
                    [12.27, 15.94],
                    [12.1, 15.99],
                    [11.92, 16],
                    [11.08, 16],
                    [10.89, 15.98],
                    [10.71, 15.93],
                    [10.54, 15.85],
                    [10.39, 15.74],
                    [10.26, 15.61],
                    [10.15, 15.46],
                    [10.07, 15.29],
                    [10.02, 15.11],
                    [10, 14.92],
                    [10, 14],
                    [11, 14],
                    [11, 12],
                    [8, 12],
                    [8, 7.09],
                    [2, 6.6],
                    [2.01, 6.43],
                    [2.11, 5.77],
                    [2.31, 5.13],
                    [2.61, 4.54],
                    [3, 4],
                    [3.33, 3.69],
                    [3.71, 3.43],
                    [4.12, 3.23],
                    [4.55, 3.08],
                    [5, 3],
                    [12, 3]
                ]
                ]
            },
            "symbol": {
                "type": "CIMPolygonSymbol",
                "symbolLayers": [
                {
                    "type": "CIMSolidFill",
                    "enable": true,
                    "color": [255, 255, 255, 255]
                }
                ],
                "angleAlignment": "Map"
            }
            }
        ],
        "scaleSymbolsProportionally": true,
        "respectFrame": true,
        "clippingPath": {
            "type": "CIMClippingPath",
            "clippingType": "Intersect",
            "path": {
            "rings": [
                [
                [0, 0],
                [21, 0],
                [21, 21],
                [0, 21],
                [0, 0]
                ]
            ]
            }
        }
        },
        {
        "type": "CIMVectorMarker",
        "enable": true,
        "name": "Group 5",
        "anchorPointUnits": "Relative",
        "dominantSizeAxis3D": "Y",
        "size": 18.75,
        "billboardMode3D": "FaceNearPlane",
        "frame": {
            "xmin": 0,
            "ymin": 0,
            "xmax": 21,
            "ymax": 21
        },
        "markerGraphics": [
            {
            "type": "CIMMarkerGraphic",
            "geometry": {
                "rings": [
                [
                    [0, 21],
                    [21, 21],
                    [21, 0],
                    [0, 0],
                    [0, 21]
                ]
                ]
            },
            "symbol": {
                "type": "CIMPolygonSymbol",
                "symbolLayers": [
                {
                    "type": "CIMSolidStroke",
                    "effects": [
                    {
                        "type": "CIMGeometricEffectOffset",
                        "method": "Square",
                        "offset": -0.5,
                        "option": "Fast"
                    }
                    ],
                    "enable": true,
                    "colorLocked": true,
                    "capStyle": "Square",
                    "joinStyle": "Miter",
                    "lineStyle3D": "Strip",
                    "miterLimit": 10,
                    "width": 1,
                    "height3D": 1,
                    "anchor3D": "Center",
                    "color": [0, 0, 0, 64]
                },
                {
                    "type": "CIMSolidFill",
                    "enable": true,
                    "color": [88, 89, 91, 255]
                }
                ],
                "angleAlignment": "Map"
            }
            }
        ],
        "scaleSymbolsProportionally": true,
        "respectFrame": true
        }
    ],
    "haloSize": 1,
    "scaleX": 1,
    "symbol3DProperties": {
        "type": "CIM3DSymbolProperties",
        "dominantSizeAxis3D": "Z",
        "rotationOrder3D": "YXZ",
        "scaleZ": 1,
        "scaleY": 1
    },
    "angleAlignment": "Display"
    }
  ```
</details>
<details>
  <summary>3. Gorilla</summary>

    ```
    {
    "type": "CIMPointSymbol",
    "symbolLayers": [
        {
        "type": "CIMVectorMarker",
        "enable": true,
        "anchorPointUnits": "Relative",
        "dominantSizeAxis3D": "Y",
        "size": 10,
        "billboardMode3D": "FaceNearPlane",
        "frame": {
            "xmin": 0,
            "ymin": 0,
            "xmax": 54,
            "ymax": 59
        },
        "markerGraphics": [
            {
            "type": "CIMMarkerGraphic",
            "geometry": {
                "paths": [
                [
                    [36.86, 23.92],
                    [38.86, 14.82],
                    [37.95, 11.32],
                    [37.43, 7.74],
                    [37.3, 4.12],
                    [35.18, 1.88],
                    [35.18, 0.5],
                    [42.57, 0.5]
                ]
                ]
            },
            "symbol": {
                "type": "CIMPolygonSymbol",
                "symbolLayers": [
                {
                    "type": "CIMSolidStroke",
                    "enable": true,
                    "capStyle": "Butt",
                    "joinStyle": "Miter",
                    "lineStyle3D": "Strip",
                    "miterLimit": 10,
                    "width": 1,
                    "color": [77, 77, 77, 255]
                }
                ]
            }
            },
            {
            "type": "CIMMarkerGraphic",
            "geometry": {
                "paths": [
                [
                    [18.92, 27.47],
                    [19.9, 26.4],
                    [21.08, 25.41],
                    [22.44, 24.53],
                    [23.94, 23.77],
                    [25.54, 23.15],
                    [27.2, 22.7],
                    [28.87, 22.42],
                    [30.51, 22.33],
                    [32.47, 22.48],
                    [34.4, 22.91],
                    [36.24, 23.62],
                    [37.95, 24.58],
                    [39.51, 25.79]
                ]
                ]
            },
            "symbol": {
                "type": "CIMPolygonSymbol",
                "symbolLayers": [
                {
                    "type": "CIMSolidStroke",
                    "enable": true,
                    "capStyle": "Butt",
                    "joinStyle": "Miter",
                    "lineStyle3D": "Strip",
                    "miterLimit": 10,
                    "width": 1,
                    "color": [77, 77, 77, 255]
                }
                ]
            }
            },
            {
            "type": "CIMMarkerGraphic",
            "geometry": {
                "paths": [
                [
                    [18.7, 31.1],
                    [18.7, 28.9],
                    [19.6, 23],
                    [19.9, 18.1],
                    [19.9, 18.1],
                    [19.5, 14.6],
                    [18.7, 11.2],
                    [16.8, 5.2],
                    [16.8, 3.4],
                    [19.1, 2.5],
                    [20.1, 1.1],
                    [20.1, 0.6],
                    [12.8, 0.6],
                    [11.2, 2.1],
                    [9.9, 8],
                    [9.1, 13.9],
                    [8.8, 19.9],
                    [9, 25.7],
                    [7.8, 33.2],
                    [7.8, 39.9],
                    [7.8, 40.9],
                    [8, 41.8],
                    [8.3, 42.7],
                    [8.8, 43.5],
                    [9.4, 44.3],
                    [10.1, 44.9]
                ]
                ]
            },
            "symbol": {
                "type": "CIMPolygonSymbol",
                "symbolLayers": [
                {
                    "type": "CIMSolidStroke",
                    "enable": true,
                    "capStyle": "Butt",
                    "joinStyle": "Miter",
                    "lineStyle3D": "Strip",
                    "miterLimit": 10,
                    "width": 1,
                    "color": [77, 77, 77, 255]
                }
                ]
            }
            },
            {
            "type": "CIMMarkerGraphic",
            "geometry": {
                "paths": [
                [
                    [4.7, 39.3],
                    [4.7, 33.2],
                    [5.8, 25.7],
                    [5.7, 19.9],
                    [6, 13.9],
                    [6.8, 8],
                    [8.1, 2.1],
                    [9.5, 0.6],
                    [12.8, 0.6]
                ]
                ]
            },
            "symbol": {
                "type": "CIMPolygonSymbol",
                "symbolLayers": [
                {
                    "type": "CIMSolidStroke",
                    "enable": true,
                    "capStyle": "Butt",
                    "joinStyle": "Miter",
                    "lineStyle3D": "Strip",
                    "miterLimit": 10,
                    "width": 1,
                    "color": [77, 77, 77, 255]
                }
                ]
            }
            },
            {
            "type": "CIMMarkerGraphic",
            "geometry": {
                "paths": [
                [
                    [39.1, 28.6],
                    [39.1, 28.6],
                    [40.6, 21.6],
                    [43.1, 14.8],
                    [43.1, 9.4],
                    [44, 4.1],
                    [42.7, 1.9],
                    [42.7, 0.5],
                    [51.6, 0.5],
                    [52.3, 4.1],
                    [52.6, 7.7],
                    [52.4, 11.1],
                    [51.7, 14.5],
                    [52.3, 17.8],
                    [53.2, 24.7],
                    [53.5, 31.6],
                    [53.3, 33.8],
                    [52.6, 35.9],
                    [51.6, 38],
                    [50.2, 39.8],
                    [48.6, 41.3],
                    [46.8, 42.4],
                    [44.9, 43],
                    [42.9, 43.2],
                    [35.5, 43.2],
                    [21.7, 48.9],
                    [16.3, 56.6],
                    [14.5, 58],
                    [12.3, 58.5],
                    [11.6, 58.5],
                    [10.9, 58.3],
                    [10.3, 58.1],
                    [9.4, 57.4],
                    [2.7, 50.4],
                    [3.2, 48.3],
                    [3.2, 46.7],
                    [3.2, 46.7],
                    [2.4, 46.3],
                    [1.6, 45.8],
                    [1.1, 45.1],
                    [0.8, 44.3],
                    [0.6, 43.5],
                    [0.5, 42.7],
                    [0.5, 42],
                    [0.7, 41.3],
                    [0.9, 40.7],
                    [1.2, 40.1],
                    [1.7, 39.6],
                    [2.2, 39.1],
                    [5, 39.3],
                    [7.8, 39.9],
                    [7.8, 39.9]
                ]
                ]
            },
            "symbol": {
                "type": "CIMPolygonSymbol",
                "symbolLayers": [
                {
                    "type": "CIMSolidStroke",
                    "enable": true,
                    "capStyle": "Butt",
                    "joinStyle": "Miter",
                    "lineStyle3D": "Strip",
                    "miterLimit": 10,
                    "width": 1,
                    "color": [77, 77, 77, 255]
                }
                ]
            }
            },
            {
            "type": "CIMMarkerGraphic",
            "geometry": {
                "paths": [
                [
                    [21.66, 48.9],
                    [20.87, 44.87]
                ]
                ]
            },
            "symbol": {
                "type": "CIMPolygonSymbol",
                "symbolLayers": [
                {
                    "type": "CIMSolidStroke",
                    "enable": true,
                    "capStyle": "Butt",
                    "joinStyle": "Miter",
                    "lineStyle3D": "Strip",
                    "miterLimit": 10,
                    "width": 1,
                    "color": [77, 77, 77, 255]
                }
                ]
            }
            }
        ],
        "scaleSymbolsProportionally": true,
        "respectFrame": true,
        "clippingPath": {
            "type": "CIMClippingPath",
            "clippingType": "Intersect",
            "path": {
            "rings": [
                [
                [0, 0],
                [54, 0],
                [54, 59],
                [0, 59],
                [0, 0]
                ]
            ]
            }
        }
        },
        {
        "type": "CIMVectorMarker",
        "enable": true,
        "anchorPointUnits": "Relative",
        "dominantSizeAxis3D": "Z",
        "size": 10,
        "billboardMode3D": "FaceNearPlane",
        "frame": {
            "xmin": 0,
            "ymin": 0,
            "xmax": 53.2,
            "ymax": 58.2
        },
        "markerGraphics": [
            {
            "type": "CIMMarkerGraphic",
            "geometry": {
                "rings": [
                [
                    [51.3, 0],
                    [51.3, 0.1],
                    [52, 3.6],
                    [52.3, 7.1],
                    [52.1, 10.6],
                    [51.4, 14.1],
                    [52, 17.4],
                    [52.9, 24.3],
                    [53.2, 31.2],
                    [53, 33.6],
                    [52.3, 35.9],
                    [51.1, 38],
                    [49.6, 39.9],
                    [48.4, 40.9],
                    [47.1, 41.8],
                    [45.7, 42.4],
                    [44.2, 42.8],
                    [42.6, 42.9],
                    [35.2, 42.9],
                    [21.4, 48.6],
                    [16.1, 56.2],
                    [15.5, 56.8],
                    [14.8, 57.3],
                    [14, 57.7],
                    [13.2, 58],
                    [12.4, 58.1],
                    [11.5, 58.2],
                    [10.6, 58],
                    [9.8, 57.8],
                    [9, 57.1],
                    [4.8, 53.1],
                    [2.2, 50.1],
                    [2.2, 50],
                    [2.8, 47.9],
                    [2.8, 46.4],
                    [2, 46],
                    [1.3, 45.5],
                    [0.8, 44.9],
                    [0.4, 44.2],
                    [0.1, 43.5],
                    [0, 42.7],
                    [0, 41.9],
                    [0.1, 41.2],
                    [0.4, 40.4],
                    [0.7, 39.7],
                    [1.2, 39.1],
                    [1.8, 38.6],
                    [1.8, 38.6],
                    [4.2, 38.7],
                    [4.2, 32.8],
                    [5.3, 25.3],
                    [5.2, 19.6],
                    [5.8, 10.6],
                    [7.6, 1.7],
                    [7.6, 1.6],
                    [9.1, 0.1],
                    [19.9, 0.1],
                    [19.9, 0.7],
                    [18.8, 2.2],
                    [16.5, 3.1],
                    [16.5, 4.7],
                    [18.4, 10.7],
                    [19.2, 14.1],
                    [19.6, 17.6],
                    [19.6, 20.1],
                    [19.3, 22.6],
                    [18.7, 26.7],
                    [21.1, 24.6],
                    [23.9, 23.1],
                    [26.9, 22.1],
                    [30.1, 21.8],
                    [32.3, 22],
                    [34.4, 22.5],
                    [36.4, 23.4],
                    [38.4, 14.5],
                    [37.2, 9.2],
                    [36.8, 3.8],
                    [34.7, 1.6],
                    [34.7, 0],
                    [51.3, 0]
                ]
                ]
            },
            "symbol": {
                "type": "CIMPolygonSymbol",
                "symbolLayers": [
                {
                    "type": "CIMSolidFill",
                    "enable": true,
                    "color": [102, 102, 102, 255]
                }
                ]
            }
            }
        ],
        "scaleSymbolsProportionally": true,
        "respectFrame": true,
        "clippingPath": {
            "type": "CIMClippingPath",
            "clippingType": "Intersect",
            "path": {
            "rings": [
                [
                [0, 0],
                [53.2, 0],
                [53.2, 58.2],
                [0, 58.2],
                [0, 0]
                ]
            ]
            }
        }
        }
    ],
    "haloSize": 1,
    "scaleX": 1,
    "angleAlignment": "Display"
    }
    ```
  </details>

WebScene Spec Symbols
- [x] <details>
  <summary>LineSymbol3DLayer</summary>
  
  Including both web spec and CIM since the latter loses the dashes
  Web spec:
  ```
    {
        "type": "LineSymbol3D",
        "symbolLayers": [
            {
                "type": "Line",
                "material": {
                    "color": [
                        0,
                        0,
                        0
                    ]
                },
                "join": "bevel",
                "cap": "round",
                "size": 3,
                "pattern": {
                    "type": "style",
                    "style": "dot"
                }
            }
        ]
    }
  ```
  CIM:
  ```
    {
        "symbolLayers": [
            {
            "capStyle": "Round",
            "color": [0, 0, 0, 255],
            "colorLocked": false,
            "enable": true,
            "joinStyle": "Round",
            "lineStyle3D": "Strip",
            "miterLimit": 10.0,
            "type": "CIMSolidStroke",
            "width": 3.0
            }
        ],
        "type": "CIMLineSymbol"
    }
  ```
  </details>
- [ ] <details>
  <summary>MeshSymbol3D</summary>
  
  ```
    Holding off on creating this symbol for now. 
  ```
  </details>
  </details>
- [x] <details>
  <summary>IconSymbol3DLayer</summary>
  
  Note: returned CIM does not store resource for icon, so this is WebSpec JSON.
  ```
    {
        "type": "PointSymbol3D",
        "symbolLayers": [
            {
                "type": "Icon",
                "material": {
                    "color": [
                        80,
                        218,
                        237
                    ]
                },
                "resource": {
                    "href": "https://static.arcgis.com/arcgis/styleItems/Icons/web/resource/Park.svg"
                },
                "size": 12.75,
                "occludedVisibility": {
                    "mode": "adaptive"
                }
            }
        ],
        "styleOrigin": {
            "styleName": "EsriIconsStyle",
            "name": "Park"
        }
    }
  ```
  </details>
- [x] <details>
  <summary>ObjectSymbol3DLayer</summary>
  
  Note: currently we do not allow for export of 3d model to JSON, so here is web scene JSON for use
  ```
    {
        "type": "PointSymbol3D",
        "symbolLayers": [
        {
            "type": "Object",
            "material": {
                "color": [
                    255,
                    222,
                    62
                ]
            },
            "resource": {
                "primitive": "invertedCone"
            },
            "height": 30
        }
        ]
    }
  ```
  </details>
- [x] <details>
  <summary>TextSymbolLayer</summary>
  
  Note: CIM export not working for text
  ```
    {
        "type": "PointSymbol3D",
        "symbolLayers": [
            {
                "type": "Text",
                "text": "text",
                "size": 18,
                "material": {
                    "color": [122, 119, 39]
                },
                "font": {
                    "family": "Arial",
                    "weight": "bold"
                }
            }
        ]
    }
  ```
  </details>

***Polygons***
- [x] <details>
  <summary>FillSymbol3DLayer</summary>
  
  ```
    {
    "symbolLayers": [
        {
            "capStyle": "Round",
            "color": [153, 153, 153, 63],
            "colorLocked": false,
            "enable": true,
            "joinStyle": "Round",
            "lineStyle3D": "Strip",
            "miterLimit": 10.0,
            "type": "CIMSolidStroke",
            "width": 0.75
        },
        {
            "color": [167, 198, 54, 255],
            "colorLocked": false,
            "enable": true,
            "type": "CIMSolidFill"
        }
    ],
        "type": "CIMPolygonSymbol"
    }

  ```
  </details>
- [x] <details>
  <summary>IconSymbol3DLayer</summary>
  
  ```
    {
        "angle": 0.0,
        "color": [0, 0, 0, 255],
        "size": 8.0,
        "style": "esriSMSCircle",
        "type": "esriSMS",
        "xoffset": 0.0,
        "yoffset": 0.0
    }

  ```
  </details>
- [ ] <details>
  <summary>ObjectSymbol3DLayer</summary>
  
  ```
    Not sure if this is currently functional. 
  ```
  </details>