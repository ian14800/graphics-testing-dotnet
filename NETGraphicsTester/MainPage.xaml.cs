using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using Esri.ArcGISRuntime.Data;

namespace NETGraphicsTester
{
    public partial class MainPage : ContentPage
    {
        GraphicsOverlay graphicsOverlay = new GraphicsOverlay();
        SimpleRenderer redCircle = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 12));
        Stopwatch drawClock = new Stopwatch();
        int[] symbolTypeCounts = new int[3] { 0, 0, 0 };

        public MainPage()
        {
            InitializeComponent();
            UpdateGraphicsCountLabel();
            _ = InitializeSceneAsync();
        }

        Random random = new Random();
        int surfacePlacement = 0;
        double currentOpacity = 1.0;
        bool overlayVisibilty = true;
        bool isBatchRun = false;
        Graphic? currentlyIdentifiedGraphic;

        private const int ModelClassMin = 1;
        private const int ModelClassMax = 7;
        private string currentScene = "Blank";
        private bool initialSetup = true;

        private string GetCurrentScene()
        {
            Picker? scenePicker = this.FindByName<Picker>("ScenePicker");
            if (scenePicker == null || scenePicker.SelectedItem == null)
            {
                return "Blank";
            }

            return scenePicker.SelectedItem.ToString() ?? "Blank";
        }

        private int GetRequestedCount()
        {
            Entry? countEntry = this.FindByName<Entry>("CountEntry");
            if (!int.TryParse(countEntry?.Text, out int count) || count <= 0)
            {
                StatusLabel.Text = "Enter a positive whole number in Count.";
                return 0;
            }

            return count;
        }

        private int GetRunsCount()
        {
            Entry? countEntry = this.FindByName<Entry>("RunsCount");
            if (!int.TryParse(countEntry?.Text, out int count) || count <= 0)
            {
                StatusLabel.Text = "Enter a positive whole number in Count.";
                return 0;
            }

            return count;
        }

        private void SceneView_DrawStatusChanged(object? sender, EventArgs e)
        {
            if (isBatchRun || sceneView == null)
            {
                return;
            }

            if (sceneView.DrawStatus == DrawStatus.Completed)
            {
                drawClock.Stop();
                DrawTimer.Text = $"{drawClock.ElapsedMilliseconds} ms // draw timer";
                UpdateGraphicsCountLabel();
            }
        }

        private void SceneView_WarningsChanged(object? sender, EventArgs e)
        {
            if (sender is Esri.ArcGISRuntime.Maui.LocalSceneView localSceneView)
            {
                LogSceneViewWarnings(localSceneView);
            }
        }

        private static void LogSceneViewWarnings(Esri.ArcGISRuntime.Maui.LocalSceneView localSceneView)
        {
            foreach (Exception warning in localSceneView.Warnings)
            {
                Debug.WriteLine($"LocalSceneView warning: {warning}");
            }
        }

        private async Task InitializeSceneAsync()
        {
            currentScene = GetCurrentScene();

            if (currentScene == "Blank")
            {
                try
                {
                    var localSceneView = sceneView;
                    if (localSceneView == null)
                    {
                        StatusLabel.Text = "Scene view is not ready.";
                        return;
                    }

                    localSceneView.WarningsChanged += SceneView_WarningsChanged;

                    //var scene = new Scene(SceneViewingMode.Local, SpatialReferences.Wgs84);
                    var scene = new Scene(SceneViewingMode.Local, BasemapStyle.ArcGISChartedTerritory);
                    var camera = new Camera(37.7, -122.4194, 15000, 0, 30, 0);

                    await scene.LoadAsync();

                    scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;

                    localSceneView.Scene = scene;
                    
                    if (initialSetup)
                    {
                        graphicsOverlay.Renderer = redCircle;
                        graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
                        graphicsOverlay.SceneProperties.AltitudeOffset = 0;
                        var overlays = localSceneView.GraphicsOverlays;
                        if (overlays == null)
                        {
                            StatusLabel.Text = "Graphics overlay collection is unavailable.";
                            return;
                        }

                        overlays.Add(graphicsOverlay);
                        initialSetup = false;
                    }

                    string PointSceneLayerUri = "https://services5.arcgis.com/N82JbI5EYtAkuUKU/arcgis/rest/services/FutureCityPoints_partialNull/SceneServer/0";
                    ArcGISSceneLayer pointSceneLayer = new ArcGISSceneLayer(new Uri(PointSceneLayerUri));

                    await pointSceneLayer.LoadAsync();
                    if (pointSceneLayer.LoadStatus != Esri.ArcGISRuntime.LoadStatus.Loaded)
                    {
                        Debug.WriteLine($"Point scene layer did not load: {pointSceneLayer.LoadStatus}");
                        return;
                    }

                    localSceneView.Scene.OperationalLayers.Add(pointSceneLayer);
                    Debug.WriteLine("Point scene layer added to operational layers.");

                    if (pointSceneLayer.Renderer?.SceneProperties is RendererSceneProperties sceneProperties)
                    {
                        sceneProperties.PitchExpression = "45";
                        Debug.WriteLine("Roll expression set to 45 for point scene layer.");
                    }
                    else
                    {
                        Debug.WriteLine("Point scene layer has no renderer scene properties; roll expression was not set.");
                    }

                    localSceneView.SetViewpointCamera(camera);
                    localSceneView.DrawStatusChanged += SceneView_DrawStatusChanged;
                    localSceneView.GeoViewTapped += OnSceneViewTapped;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error thrown: {ex.Message}");
                }
            } else
            {
                string sceneId = "";
                switch(currentScene)
                {
                    case "Heavy":
                        sceneId = "9851fee6ec0544e0a0c2884d2297c349";
                        break;
                    case "Medium":
                        sceneId = "e10369820fa04866a16338cce14c0f36";
                        break;
                    case "Light":
                        sceneId = "3023142800a64ea1b6ec3459c05c9ac0";
                        break;
                    default:
                        sceneId = "3023142800a64ea1b6ec3459c05c9ac0";
                        break;
                }

                try
                {
                    ArcGISPortal portal = await ArcGISPortal.CreateAsync();

                    PortalItem sceneItem = await PortalItem.CreateAsync(portal, sceneId);

                    Scene scene = new Scene(sceneItem);

                    sceneView.Scene = scene;
                } catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading scene: {ex.Message}");
                }
            }  
        }

        private void OnScenePickerSelectionChanged(object sender, EventArgs e)
        {
            string selectedScene = GetCurrentScene();
            if (selectedScene != currentScene)
            {
                currentScene = selectedScene;
                _ = InitializeSceneAsync();
            }
        }

        private void UpdateGraphicsCountLabel()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Label? graphicsCountLabel = this.FindByName<Label>("GraphicsCountLabel");
                if (graphicsCountLabel != null)
                {
                    graphicsCountLabel.Text = $"Graphics: {graphicsOverlay.Graphics.Count}";
                }
            });
        }

        private async void OnAddPointGraphicsClicked(object sender, EventArgs e)
        {
            await AddGraphicsOperation(updateUi: true, geomType: "point");
        }

        private static void LogGraphicsDiagnostic(string message)
        {
            string entry = $"{DateTimeOffset.Now:O} [thread {Environment.CurrentManagedThreadId}] {message}";
            Debug.WriteLine(entry);
        }

        private async void OnAddLineGraphicsClicked(object sender, EventArgs e)
        {
            await AddGraphicsOperation(updateUi: true, geomType: "line");
        }

        private async void OnAddPolygonGraphicsClicked(object sender, EventArgs e)
        {
            await AddGraphicsOperation(updateUi: true, geomType: "polygon");
        }

        private void OnToggleManualOperationsClicked(object sender, EventArgs e)
        {
            ManualOperationsPanel.IsVisible = !ManualOperationsPanel.IsVisible;
            ToggleManualOperationsButton.Text = ManualOperationsPanel.IsVisible
                ? "Hide controls"
                : "Show controls";
        }

        async private Task AddGraphicsOperation(bool updateUi, string geomType)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            Envelope? extent = sceneView.GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry as Envelope;
            if (extent == null)
            {
                System.Diagnostics.Debug.WriteLine("Could not determine current viewpoint extent.");
                StatusLabel.Text = "Could not determine current viewpoint extent.";
                return;
            }

            MapPoint? center = sceneView.GetCurrentViewpoint(ViewpointType.CenterAndScale)?.TargetGeometry as MapPoint;
            double baseZ = center?.Z ?? 0;
            const double zRange = 500;

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }

            if (geomType == "point")
            {
                string stage = "starting point graphics";
                int originalCount = graphicsOverlay.Graphics.Count;
                try
                {
                    SimpleRenderer pointRenderer = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 10));
                    graphicsOverlay.Renderer = pointRenderer;
                    SymbolJsonEditor.Text = pointRenderer.ToJson();
                    for (int i = 0; i < count; i++)
                    {
                        stage = $"constructing point {i + 1}/{count}";
                        double x = extent.XMin + (random.NextDouble() * (extent.XMax - extent.XMin));
                        double y = extent.YMin + (random.NextDouble() * (extent.YMax - extent.YMin));
                        double z = baseZ + ((random.NextDouble() * 2 * zRange) - zRange);
                        MapPoint point = new MapPoint(x, y, z, extent.SpatialReference);

                        Graphic graphic = new Graphic(point);
                        int symbolCount = 57;
                        graphic.Attributes["class_value"] = ((i + 1) % symbolCount) + 1;
                        graphic.Attributes["size_value"] = random.Next(0, 100 + 1);
                        graphic.Attributes["transparency_value"] = random.Next(0, 100 + 1);
                        graphic.Attributes["rotation_value"] = "50";
                        graphic.Attributes["color_value"] = random.Next(0, 100 + 1);
                        stage = $"adding point {i + 1}/{count}";
                        if (i == 0 || i == count - 1)
                        {
                            LogGraphicsDiagnostic($"Before {stage}: ({x}, {y}, {z})");
                        }
                        graphicsOverlay.Graphics.Add(graphic);
                    }

                }
                catch (Exception ex)
                {
                    drawClock.Stop();
                    if (updateUi)
                    {
                        StatusLabel.Text = $"Point operation failed at {stage}: {ex.Message}";
                    }
                }
                finally
                {
                    operationTimer.Stop();
                    if (updateUi)
                    {
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                    }
                }
            }
            else if (geomType == "line")
            {
                SimpleRenderer lineRenderer = new SimpleRenderer(new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 5));
                graphicsOverlay.Renderer = lineRenderer;
                //SymbolJsonEditor.Text = lineRenderer.ToJson();
                double xRange = extent.XMax - extent.XMin;
                double yRange = extent.YMax - extent.YMin;
                double xStep = xRange / (count - 1);
                double yStep = yRange / (count - 1);
                for (int i = 0; i < count; i++)
                {

                    double currentXMin = extent.XMin + (i * xStep);
                    double currentXMax = extent.XMin + ((i + 1) * xStep);
                    // double currentYMin = extent.YMin + (i * yStep);
                    // double currentYMax = extent.YMin + ((i + 1) * yStep);

                    double x1 = currentXMin;
                    double y1 = extent.YMin;
                    double z1 = baseZ + ((random.NextDouble() * 2 * zRange) - zRange);
                    MapPoint point1 = new MapPoint(x1, y1, z1, extent.SpatialReference);

                    double x2 = currentXMax;
                    double y2 = extent.YMax;
                    double z2 = baseZ + ((random.NextDouble() * 2 * zRange) - zRange);
                    MapPoint point2 = new MapPoint(x2, y2, z2, extent.SpatialReference);

                    Polyline line = new Polyline(new MapPoint[] { point1, point2 }, extent.SpatialReference);

                    Graphic graphic = new Graphic(line);
                    int symbolCount = 33;
                    graphic.Attributes["class_value"] = ((i + 1) % symbolCount) + 1;
                    graphic.Attributes["size_value"] = random.Next(0, 100 + 1);
                    graphic.Attributes["transparency_value"] = random.Next(0, 100 + 1);
                    graphic.Attributes["rotation_value"] = "50";
                    graphic.Attributes["color_value"] = random.Next(0, 100 + 1);
                    graphicsOverlay.Graphics.Add(graphic);
                }
                operationTimer.Stop();

                if (updateUi)
                {
                    EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                    StatusLabel.Text = $"Added {count} line graphics.";
                }
            }
            else if (geomType == "polygon")
            {
                // SimpleRenderer interimRenderer = new SimpleRenderer(new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.Orange, null));
                // graphicsOverlay.Renderer = interimRenderer;
                // //SymbolJsonEditor.Text = interimRenderer.ToJson();
                // double xRange = extent.XMax - extent.XMin;
                // double yRange = extent.YMax - extent.YMin;
                // double xStep = xRange / (count - 1);
                // double yStep = yRange / (count - 1);
                // for (int i = 0; i < count; i++)
                // {
                //     double currentXMin = extent.XMin + (i * xStep);
                //     double currentXMax = extent.XMin + ((i + 1) * xStep);
                //     // double currentYMin = extent.YMin + (i * yStep);
                //     // double currentYMax = extent.YMin + ((i + 1) * yStep);

                //     double x1 = currentXMin;
                //     double y1 = extent.YMin;
                //     double z1 = baseZ + ((random.NextDouble() * 2 * zRange) - zRange);
                //     MapPoint point1 = new MapPoint(x1, y1, z1, extent.SpatialReference);

                //     double x2 = currentXMax;
                //     double y2 = extent.YMin;
                //     double z2 = baseZ + ((random.NextDouble() * 2 * zRange) - zRange);
                //     MapPoint point2 = new MapPoint(x2, y2, z2, extent.SpatialReference);
                                        
                //     double x3 = currentXMax;
                //     double y3 = extent.YMax;
                //     double z3 = baseZ + ((random.NextDouble() * 2 * zRange) - zRange);
                //     MapPoint point3 = new MapPoint(x3, y3, z3, extent.SpatialReference);

                //     Polygon polygon = new Polygon(new MapPoint[] { point1, point2, point3 }, extent.SpatialReference);

                //     Graphic graphic = new Graphic(polygon);
                //     int symbolCount = 8;
                //     graphic.Attributes["class_value"] = ((i + 1) % symbolCount) + 1;
                //     graphic.Attributes["size_value"] = random.Next(0, 100 + 1);
                //     graphic.Attributes["transparency_value"] = random.Next(0, 100 + 1);
                //     graphic.Attributes["rotation_value"] = "50";
                //     graphic.Attributes["color_value"] = random.Next(0, 100 + 1);
                //     graphicsOverlay.Graphics.Add(graphic);
                // }

                try
                {
                    using var sourceStream = await FileSystem.OpenAppPackageFileAsync("SF_polygons.json");
                    using var reader = new StreamReader(sourceStream);
                    JsonNode? featureSet = JsonNode.Parse(await reader.ReadToEndAsync());
                    JsonArray? features = featureSet?["features"]?.AsArray();
                    JsonNode? spatialReference = featureSet?["spatialReference"];
                    if (features == null || spatialReference == null)
                    {
                        throw new InvalidDataException("The polygon JSON is missing features or a spatial reference.");
                    }

                    int polygonCount = Math.Min(count, features.Count);
                    for (int i = 0; i < polygonCount; i++)
                    {
                        if (features[i]?["geometry"]?.DeepClone() is not JsonObject geometryJson)
                        {
                            continue;
                        }

                        geometryJson["spatialReference"] = spatialReference.DeepClone();
                        Geometry geometry = Geometry.FromJson(geometryJson.ToJsonString());
                        var fillColor = System.Drawing.Color.FromArgb(
                            random.Next(40, 231),
                            random.Next(40, 231),
                            random.Next(40, 231));
                        var graphic = new Graphic(geometry)
                        {
                            Symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, fillColor, null)
                        };
                        graphicsOverlay.Graphics.Add(graphic);
                    }

                    operationTimer.Stop();
                    if (updateUi)
                    {
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                        StatusLabel.Text = $"Added {polygonCount} polygon graphics.";
                    }
                }
                catch (Exception ex)
                {
                    operationTimer.Stop();
                    if (updateUi)
                    {
                        StatusLabel.Text = $"Polygon operation failed: {ex.Message}";
                    }
                }
            }
        }

        private void OnRemoveGraphicsClicked(object sender, EventArgs e)
        {
            RemoveGraphicsOperation(updateUi: true);
        }

        private void RemoveGraphicsOperation(bool updateUi)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            int removeCount = Math.Min(count, graphicsOverlay.Graphics.Count);

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }


            for (int i = 0; i < removeCount; i++)
            {
                graphicsOverlay.Graphics.RemoveAt(graphicsOverlay.Graphics.Count - 1);
            }

            operationTimer.Stop();
            if (updateUi)
            {
                EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                StatusLabel.Text = $"Removed {removeCount} graphics.";
            }
        }

        private async void OnRendererSelectionChanged(object sender, EventArgs e)
        {
            if (sender is not Picker picker || picker.SelectedIndex < 0)
            {
                return;
            }

            const int jsonRendererIndex = 6;
            JsonRendererPanel.IsVisible = picker.SelectedIndex == jsonRendererIndex;

            if (picker.SelectedIndex != jsonRendererIndex)
            {
                await SwapRendererOperationAsync(picker.SelectedIndex, updateUi: true);
            }
        }

        private async Task SwapRendererOperationAsync(int rendererIndex, bool updateUi)
        {
            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }


            switch (rendererIndex)
            {
                case 0:
                    graphicsOverlay.Renderer = redCircle;
                    operationTimer.Stop();
                    if (updateUi)
                    {
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                        StatusLabel.Text = $"Renderer set to simple red circle.";
                    }
                    break;
                case 1:
                    SimpleRenderer blueSquare = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Blue, 20));
                    graphicsOverlay.Renderer = blueSquare;
                    operationTimer.Stop();
                    if (updateUi)
                    {
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                        StatusLabel.Text = $"Renderer set to simple red circle.";
                    }
                    break;
                case 2:
                    try
                    {
                        UniqueValueRenderer uniqueValueModels = new UniqueValueRenderer();
                        uniqueValueModels.FieldNames.Add("class_value");

                        MultilayerPointSymbol modelSymbol1 = await createModelLayerFromFile("1");
                        uniqueValueModels.UniqueValues.Add(new UniqueValue("Model 1", "1.glb", modelSymbol1, "1"));

                        MultilayerPointSymbol modelSymbol2 = await createModelLayerFromFile("2");
                        uniqueValueModels.UniqueValues.Add(new UniqueValue("Model 2", "2.glb", modelSymbol2, "2"));

                        MultilayerPointSymbol modelSymbol3 = await createModelLayerFromFile("3");
                        uniqueValueModels.UniqueValues.Add(new UniqueValue("Model 3", "3.glb", modelSymbol3, "3"));

                        MultilayerPointSymbol modelSymbol4 = await createModelLayerFromFile("4");
                        uniqueValueModels.UniqueValues.Add(new UniqueValue("Model 4", "4.glb", modelSymbol4, "4"));

                        MultilayerPointSymbol modelSymbol5 = await createModelLayerFromFile("5");
                        uniqueValueModels.UniqueValues.Add(new UniqueValue("Model 5", "5.glb", modelSymbol5, "5"));

                        uniqueValueModels.DefaultSymbol = modelSymbol1;
                        uniqueValueModels.DefaultLabel = "Default model";

                        graphicsOverlay.Renderer = uniqueValueModels;
                        operationTimer.Stop();
                        if (updateUi)
                        {
                            EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                            StatusLabel.Text = $"Renderer set to 3D model unique values.";
                        }
                    }
                    catch (Exception ex)
                    {
                        operationTimer.Stop();
                        if (updateUi)
                        {
                            EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                            StatusLabel.Text = $"Model renderer failed: {ex.Message}";
                        }
                    }
                    break;
                case 3:
                    try
                    {
                        UniqueValueRenderer uniqueValuePictures = new UniqueValueRenderer();
                        uniqueValuePictures.FieldNames.Add("class_value");

                        MultilayerPointSymbol pictureSymbol1 = await createPictureLayerFromFile("1");
                        uniqueValuePictures.UniqueValues.Add(new UniqueValue("Picture 1", "1.png", pictureSymbol1, "1"));

                        MultilayerPointSymbol pictureSymbol2 = await createPictureLayerFromFile("2");
                        uniqueValuePictures.UniqueValues.Add(new UniqueValue("Picture 2", "2.png", pictureSymbol2, "2"));

                        MultilayerPointSymbol pictureSymbol3 = await createPictureLayerFromFile("3");
                        uniqueValuePictures.UniqueValues.Add(new UniqueValue("Picture 3", "3.png", pictureSymbol3, "3"));

                        MultilayerPointSymbol pictureSymbol4 = await createPictureLayerFromFile("4");
                        uniqueValuePictures.UniqueValues.Add(new UniqueValue("Picture 4", "4.png", pictureSymbol4, "4"));

                        MultilayerPointSymbol pictureSymbol5 = await createPictureLayerFromFile("5");
                        uniqueValuePictures.UniqueValues.Add(new UniqueValue("Picture 5", "5.png", pictureSymbol5, "5"));


                        uniqueValuePictures.DefaultSymbol = pictureSymbol1;
                        uniqueValuePictures.DefaultLabel = "Default Picture";

                        graphicsOverlay.Renderer = uniqueValuePictures;
                        operationTimer.Stop();
                        if (updateUi)
                        {
                            EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                            StatusLabel.Text = $"Renderer set to picture unique values.";
                        }
                    }
                    catch (Exception ex)
                    {
                        operationTimer.Stop();
                        if (updateUi)
                        {
                            EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                            StatusLabel.Text = $"Picture renderer failed: {ex.Message}";
                        }
                    }
                    break;
                case 4:
                    string symbolFilePath = "MinShipSymbols_round1_points.stylx";
                    SymbolStyle _pointSymbols = await SymbolStyle.OpenAsync(symbolFilePath);

                    SymbolStyleSearchParameters searchParams = await _pointSymbols.GetDefaultSearchParametersAsync();
                    SymbolStyleSearchParameters searchParams1 = new SymbolStyleSearchParameters();

                    IList<SymbolStyleSearchResult> styleResults = await _pointSymbols.SearchSymbolsAsync(searchParams1);

                    UniqueValueRenderer styleUniqueRenderer = new UniqueValueRenderer();
                    styleUniqueRenderer.FieldNames.Add("class_value");

                    int symbolIndex = 1;

                    foreach (SymbolStyleSearchResult result in styleResults)
                    {
                        // MultilayerPointSymbol multiLayerSym = await result.GetSymbolAsync() as MultilayerPointSymbol;
                        Symbol multiLayerSym = await result.GetSymbolAsync();

                        styleUniqueRenderer.UniqueValues.Add(new UniqueValue(result.Name, result.Key, multiLayerSym, symbolIndex.ToString()));

                        Debug.WriteLine($"Added symbol to renderer: {result.Name}, type: {multiLayerSym.GetType().Name ?? "Unknown"}");

                        symbolIndex++;
                    }

                    graphicsOverlay.Renderer = styleUniqueRenderer;
                    break;
                case 5:
                    ClassBreaksRenderer classBreaksRenderer = new ClassBreaksRenderer();

                    classBreaksRenderer.FieldName = "class_value";
                    Symbol lowSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Green, 10);
                    Symbol mediumSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Yellow, 15);
                    Symbol highSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 20);
                    classBreaksRenderer.ClassBreaks.Add(new ClassBreak("Low", "0-1", 0, 1, lowSymbol));
                    classBreaksRenderer.ClassBreaks.Add(new ClassBreak("Medium", "2-3", 2, 3, mediumSymbol));
                    classBreaksRenderer.ClassBreaks.Add(new ClassBreak("High", "4-9", 4, 9, highSymbol));

                    graphicsOverlay.Renderer = classBreaksRenderer;
                    operationTimer.Stop();

                    if (updateUi)
                    {
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                        StatusLabel.Text = $"Renderer set to class breaks.";
                    }
                    break;
                default:
                    graphicsOverlay.Renderer = redCircle;
                    operationTimer.Stop();
                    if (updateUi)
                    {
                        EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                        StatusLabel.Text = $"Renderer set to simple red circle.";
                    }
                    break;
            }
        }

        private void OnApplyJsonRendererClicked(object sender, EventArgs e)
        {
            string json = RendererJsonEditor.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(json))
            {
                StatusLabel.Text = "Enter renderer JSON.";
                return;
            }

            try
            {
                Renderer? renderer = Renderer.FromJson(json);
                if (renderer == null)
                {
                    StatusLabel.Text = "Renderer JSON did not produce a renderer.";
                    return;
                }

                graphicsOverlay.Renderer = renderer;
                StatusLabel.Text = "JSON renderer applied.";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Invalid renderer JSON: {ex.Message}";
            }
        }

        private void OnApplyJsonSymbolClicked(object sender, EventArgs e)
        {
            string json = (SymbolJsonEditor.Text ?? string.Empty).Trim().Trim('"', '\'');
            string sizeUp = json.Replace("\"size\":16", "\"size\":42");
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sizeUp))
            {
                StatusLabel.Text = "Enter symbol JSON.";
                return;
            }

            try
            {
                Symbol? symbol = Symbol.FromJson(sizeUp);

                if (symbol == null)
                {
                    StatusLabel.Text = "Symbol JSON did not produce a symbol.";
                    return;
                }

                SimpleLineSymbol simpleLine = symbol as SimpleLineSymbol;
                if (simpleLine != null)
                {
                    Debug.WriteLine("Converting to multilayer line symbol");
                    MultilayerPolylineSymbol multiLayerLine = simpleLine.ToMultilayerSymbol();
                    for (int i = 0; i < count; i++)
                    {
                        graphicsOverlay.Graphics[i].Symbol = multiLayerLine;
                    }
                    return;
                }
                PictureFillSymbol pictureFillSymbol = symbol as PictureFillSymbol;
                if (pictureFillSymbol != null)
                {
                    Debug.WriteLine("Converting to multilayer picture fill symbol");
                    MultilayerPolygonSymbol multiLayerPolygon = pictureFillSymbol.ToMultilayerSymbol();
                    for (int i = 0; i < count; i++)
                    {
                        graphicsOverlay.Graphics[i].Symbol = multiLayerPolygon;
                    }
                    return;
                }
                SimpleFillSymbol simpleFillSymbol = symbol as SimpleFillSymbol;
                if (simpleFillSymbol != null)
                {
                    Debug.WriteLine("Converting to multilayer simple fill symbol");
                    MultilayerPolygonSymbol multiLayerPolygon = simpleFillSymbol.ToMultilayerSymbol();
                    for (int i = 0; i < count; i++)
                    {
                        graphicsOverlay.Graphics[i].Symbol = multiLayerPolygon;
                    }
                    return;
                }
                SimpleMarkerSymbol simpleMarker = symbol as SimpleMarkerSymbol;
                if (simpleLine != null)
                {
                    Debug.WriteLine("Converting to multilayer marker symbol");
                    MultilayerPointSymbol multiLayerMarker = simpleMarker.ToMultilayerSymbol();
                    for (int i = 0; i < count; i++)
                    {
                        graphicsOverlay.Graphics[i].Symbol = multiLayerMarker;
                    }
                    return;
                }
                PictureMarkerSymbol pictureMarker = symbol as PictureMarkerSymbol;
                if (simpleLine != null)
                {
                    Debug.WriteLine("Converting to multilayer picture marker symbol");
                    MultilayerPointSymbol multiLayerPictureMarker = pictureMarker.ToMultilayerSymbol();
                    for (int i = 0; i < count; i++)
                    {
                        graphicsOverlay.Graphics[i].Symbol = multiLayerPictureMarker;
                    }
                    return;
                }
                    
                Debug.WriteLine("Symbol cannot be converted to multilayer explicitly");


                for (int i = 0; i < count; i++)
                {
                    graphicsOverlay.Graphics[i].Symbol = symbol;
                }
                StatusLabel.Text = "JSON symbol applied";

                SymbolJsonEditor.Text = symbol.ToJson();
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Invalid symbol JSON: {ex.Message}";
            }
        }

        private async void OnSwapPlacementClicked(object sender, EventArgs e)
        {
            await SwapPlacementOperationAsync(updateUi: true);
        }

        private async Task SwapPlacementOperationAsync(bool updateUi)
        {
            surfacePlacement++;
            if (surfacePlacement > 4)
            {
                surfacePlacement = 0;
            }

            switch (surfacePlacement)
            {
                case 0:
                    graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
                    if (updateUi)
                    {
                        StatusLabel.Text = "Overlay's surface placement updated to absolute.";
                    }
                    break;
                case 1:
                    graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Relative;
                    if (updateUi)
                    {
                        StatusLabel.Text = "Overlay's surface placement updated to relative.";
                    }
                    break;
                case 2:
                    graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.DrapedBillboarded;
                    if (updateUi)
                    {
                        StatusLabel.Text = "Overlay's surface placement updated to draped billboarded.";
                    }
                    break;
                case 3:
                    graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.DrapedFlat;
                    if (updateUi)
                    {
                        StatusLabel.Text = "Overlay's surface placement updated to draped flat.";
                    }
                    break;
       
                case 4:
                    graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.RelativeToScene;
                    if (updateUi)
                    {
                        StatusLabel.Text = "Overlay's surface placement updated to relative to scene.";
                    }
                    break;
                default:
                    graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
                    if (updateUi)
                    {
                        StatusLabel.Text = "Overlay's surface placement updated to absolute.";
                    }
                    break;

                    
            }
        }

        private async void OnAddAltitudeOffsetClicked(object sender, EventArgs e)
        {
            await AddAltitudeOffsetOperationAsync(updateUi: true);
        }

        private async Task AddAltitudeOffsetOperationAsync(bool updateUi)
        {
            graphicsOverlay.SceneProperties.AltitudeOffset += 10000;
            if (updateUi)
            {
                StatusLabel.Text = $"Altitude offset increased to: {graphicsOverlay.SceneProperties.AltitudeOffset}";
            }
        }

        async private Task<MultilayerPointSymbol> createModelLayerFromFile(string fieldValue, int size = 50)
        {
            string file = $"{fieldValue}.glb";

            string filePath = Path.Combine(FileSystem.CacheDirectory, file);
            {
                using var sourceStream = await FileSystem.OpenAppPackageFileAsync(file);
                using var destStream = File.Create(filePath);
                await sourceStream.CopyToAsync(destStream);
            }

            ModelSymbolLayer modelLayer = new ModelSymbolLayer(new Uri(filePath, UriKind.Absolute));
            await modelLayer.LoadAsync();
            Debug.WriteLine($"{file} default dimensions: height={modelLayer.Height}, width={modelLayer.Width}, depth={modelLayer.Depth}");

            MultilayerPointSymbol multilayerSymbol = new MultilayerPointSymbol(new SymbolLayer[] { modelLayer })
            {
                SceneSizeUnit = SymbolSizeUnits.Meters,
                Size = size
            };

            return multilayerSymbol;
        }

        async private Task<MultilayerPointSymbol> createPictureLayerFromFile(string fieldValue, double size = 80.0)
        {
            string file = $"{fieldValue}.png";

            string picturePath = Path.Combine(FileSystem.CacheDirectory, file);
            if (!File.Exists(picturePath))
            {
                using var sourceStream = await FileSystem.OpenAppPackageFileAsync(file);
                using var destStream = File.Create(picturePath);
                await sourceStream.CopyToAsync(destStream);
            }
            PictureMarkerSymbolLayer pictureLayer = new PictureMarkerSymbolLayer(new Uri(picturePath, UriKind.Absolute)) { Size = size };
            MultilayerPointSymbol multilayerSymbol = new MultilayerPointSymbol(new SymbolLayer[] { pictureLayer });
            return multilayerSymbol;
        }

        private void OnMoveGraphicsUniformClicked(object sender, EventArgs e)
        {
            MoveGraphicsUniformOperation(updateUi: true);
        }

        private void MoveGraphicsUniformOperation(bool updateUi)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            double dx = random.Next(-1000, 1000);
            double dy = random.Next(-1000, 1000);
            double dz = random.Next(-1000, 1000);

            int graphicsToMove = Math.Min(count, graphicsOverlay.Graphics.Count);

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }

            for (int i = 0; i < graphicsToMove; i++)
            {
                Graphic graphic = graphicsOverlay.Graphics[i];

                if (graphic.Geometry is MapPoint currentPoint)
                {
                    MapPoint movedPoint = new MapPoint(
                        currentPoint.X + dx,
                        currentPoint.Y + dy,
                        currentPoint.Z + dz,
                        currentPoint.SpatialReference);

                    graphic.Geometry = movedPoint;
                }
            }

            operationTimer.Stop();
            if (updateUi)
            {
                EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                StatusLabel.Text = $"Moved {graphicsToMove} graphics by ({dx:0}, {dy:0}, {dz:0}).";
            }
        }

        private void OnMoveGraphicsRandomClicked(object sender, EventArgs e)
        {
            MoveGraphicsRandomOperation(updateUi: true);
        }

        private void MoveGraphicsRandomOperation(bool updateUi)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            int graphicsToMove = Math.Min(count, graphicsOverlay.Graphics.Count);

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }


            for (int i = 0; i < graphicsToMove; i++)
            {
                double dx = random.Next(-100, 100);
                double dy = random.Next(-100, 100);
                double dz = random.Next(-100, 100);
                Graphic graphic = graphicsOverlay.Graphics[i];

                if (graphic.Geometry is MapPoint currentPoint)
                {
                    MapPoint movedPoint = new MapPoint(
                        currentPoint.X + dx,
                        currentPoint.Y + dy,
                        currentPoint.Z + dz,
                        currentPoint.SpatialReference);

                    graphic.Geometry = movedPoint;
                }
            }

            operationTimer.Stop();
            if (updateUi)
            {
                EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                StatusLabel.Text = $"Moved {graphicsToMove} graphics randomly.";
            }
        }

        async private void OnAddSymbolsClicked(object sender, EventArgs e)
        {
            await AddSymbolsOperationAsync(updateUi: true);
        }

        private async Task AddSymbolsOperationAsync(bool updateUi)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            int assignCount = Math.Min(count, graphicsOverlay.Graphics.Count);

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }

            string symbolFilePath = "MinShipSymbols_round2_points.stylx";
            SymbolStyle _pointSymbols = await SymbolStyle.OpenAsync(symbolFilePath);

            SymbolStyleSearchParameters searchParams = await _pointSymbols.GetDefaultSearchParametersAsync();

            IList<SymbolStyleSearchResult> styleResults = await _pointSymbols.SearchSymbolsAsync(searchParams);


            IList<Symbol> symbolList = new List<Symbol>();

            foreach (SymbolStyleSearchResult result in styleResults)
            {
                Symbol symbol = await result.GetSymbolAsync();

                symbolList.Add(symbol);
            }

            Debug.WriteLine($"Built list of {symbolList.Count} symbols");

            for (int i = 0; i < assignCount; i++)
            {
                int symbolCount = symbolList.Count;

                int symbolType = random.Next(1, symbolCount);
                Graphic graphic = graphicsOverlay.Graphics[i];

                Symbol nextUpSymbol = symbolList[symbolType];

                try
                {
                    graphic.Symbol = nextUpSymbol;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error applying symbol: {ex.Message}");
                }
                    

            }

            operationTimer.Stop();
            if (updateUi)
            {
                EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                StatusLabel.Text = $"Assigned symbols to {assignCount} graphics. Symbol type ratio: {symbolTypeCounts[0]}S / {symbolTypeCounts[1]} M / {symbolTypeCounts[2]} P";
                symbolTypeCounts[0] = 0;
                symbolTypeCounts[1] = 0;
                symbolTypeCounts[2] = 0;
            }
        }

        private void OnRemoveOverlayClicked(object sender, EventArgs e)
        {
            RemoveOverlayOperation(updateUi: true);
        }

        private void RemoveOverlayOperation(bool updateUi)
        {
            var localSceneView = sceneView;
            if (localSceneView == null)
            {
                return;
            }

            var overlays = localSceneView.GraphicsOverlays;
            if (overlays == null)
            {
                return;
            }

            overlays.Remove(graphicsOverlay);

            if (updateUi)
            {
                StatusLabel.Text = $"Removed overlay.";
            }
        }
        private void OnAddOverlayClicked(object sender, EventArgs e)
        {
            AddOverlayOperation(updateUi: true);
        }

        private void AddOverlayOperation(bool updateUi)
        {
            var localSceneView = sceneView;
            if (localSceneView == null)
            {
                return;
            }

            var overlays = localSceneView.GraphicsOverlays;
            if (overlays == null)
            {
                return;
            }

            overlays.Add(graphicsOverlay);

            if (updateUi)
            {
                StatusLabel.Text = $"Added overlay.";
            }
        }

        private void OnToggleVisibiltyClicked(object sender, EventArgs e)
        {
            ToggleVisibilityOperation(updateUi: true);
        }

        private void ToggleVisibilityOperation(bool updateUi)
        {
            overlayVisibilty = !overlayVisibilty;

            graphicsOverlay.IsVisible = overlayVisibilty;

            if (updateUi)
            {
                StatusLabel.Text = $"Overlay visibilty set to {overlayVisibilty}";
            }
        }

        private void OnToggleOpacityClicked(object sender, EventArgs e)
        {
            ToggleOpacityOperation(updateUi: true);
        }

        private void ToggleOpacityOperation(bool updateUi)
        {
            if (currentOpacity == 0.0)
            {
                currentOpacity = 1.0;
            } else
            {
                currentOpacity -= .25;
            }
      
            graphicsOverlay.Opacity = currentOpacity;

            if (updateUi)
            {
                StatusLabel.Text = $"Overlay opacity is set to {currentOpacity}";
            }
        }

        private void OnToggleGraphicsVisibilityClicked(object sender, EventArgs e)
        {
            ToggleGraphicsVisibilityOperation(updateUi: true);
        }

        private void ToggleGraphicsVisibilityOperation(bool updateUi)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            int removeCount = Math.Min(count, graphicsOverlay.Graphics.Count);

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }


            for (int i = 0; i < removeCount; i++)
            {
                if (graphicsOverlay.Graphics[i].IsVisible)
                {
                    graphicsOverlay.Graphics[i].IsVisible = false;
                } else
                {
                    graphicsOverlay.Graphics[i].IsVisible = true;
                }
            }

            operationTimer.Stop();
            if (updateUi)
            {
                EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                StatusLabel.Text = $"Toggled visibility for {removeCount} graphics.";
            }
        }

        private void OnToggleGraphicsSelectedClicked(object sender, EventArgs e)
        {
            ToggleGraphicsSelectedOperation(updateUi: true);
        }

        private void ToggleGraphicsSelectedOperation(bool updateUi)
        {
            int count = GetRequestedCount();
            if (count <= 0)
            {
                return;
            }

            int removeCount = Math.Min(count, graphicsOverlay.Graphics.Count);

            if (updateUi)
            {
                EventTimer.Text = " // event time";
                DrawTimer.Text = " // draw time";
            }
            Stopwatch operationTimer = Stopwatch.StartNew();
            if (updateUi)
            {
                drawClock.Reset();
                drawClock.Start();
            }


            for (int i = 0; i < removeCount; i++)
            {
                if (graphicsOverlay.Graphics[i].IsSelected)
                {
                    graphicsOverlay.Graphics[i].IsSelected = false;
                }
                else
                {
                    graphicsOverlay.Graphics[i].IsSelected = true;
                }
            }

            operationTimer.Stop();
            if (updateUi)
            {
                EventTimer.Text = $"{operationTimer.ElapsedMilliseconds} ms // event time";
                StatusLabel.Text = $"Toggled selection for {removeCount} graphics.";
            }
        }

        private async void OnRunAnimationsClicked(object sender, EventArgs e)
        {
            int runsCount = GetRunsCount();
            int count = GetRequestedCount();
            string workflow = WorkflowPicker.SelectedItem?.ToString() ?? "None";

            if (count <= 0 || runsCount <= 0)
            {
                return;
            }

            EventTimer.Text = " // event time";
            DrawTimer.Text = " // draw time";
            Stopwatch operationTimer = Stopwatch.StartNew();
            Stopwatch runTimer = new Stopwatch();

            double[] runTimes = new double[runsCount];
            isBatchRun = true;

            try
            {
                for (int i = 0; i < runsCount; i++)
                {
                    runTimer.Restart();
                    switch (workflow)
                    {
                        case "Move graphics (uniform)":
                            MoveGraphicsUniformOperation(updateUi: false);
                            break;
                        case "Move graphics (random)":
                            MoveGraphicsRandomOperation(updateUi: false);
                            break;
                        // case "Swap renderer":
                        //     await SwapRendererOperationAsync(updateUi: false);
                        //     break;
                        case "Add symbols":
                            await AddSymbolsOperationAsync(updateUi: false);
                            break;
                        case "Add/remove overlay":
                            RemoveOverlayOperation(updateUi: false);
                            AddOverlayOperation(updateUi: false);
                            break;
                        case "Add/move/remove":
                            //AddGraphicsOperation(updateUi: false);
                            MoveGraphicsRandomOperation(updateUi: false);
                            RemoveGraphicsOperation(updateUi: false);
                            break;
                        case "Add/give symbols/remove":
                            //AddGraphicsOperation(updateUi: false);
                            await AddSymbolsOperationAsync(updateUi: false);
                            RemoveGraphicsOperation(updateUi: false);
                            break;
                        case "Add/give symbols/remove overlay":
                            //AddGraphicsOperation(updateUi: false);
                            await AddSymbolsOperationAsync(updateUi: false);
                            RemoveOverlayOperation(updateUi: false);
                            RemoveGraphicsOperation(updateUi: false);
                            AddOverlayOperation(updateUi: false);
                            break;
                        default:
                            StatusLabel.Text = $"Unknown workflow: {workflow}";
                            return;
                    }
                    runTimer.Stop();
                    runTimes[i] = runTimer.Elapsed.TotalMilliseconds;
                }
            }
            finally
            {
                isBatchRun = false;
            }

            operationTimer.Stop();
            EventTimer.Text = $"Total time: {operationTimer.ElapsedMilliseconds} ms // slowest time: {runTimes.Max()} // fastest time: {runTimes.Min()} // average time: {runTimes.Average()}";

            try
            {
                //string csvPath = await SaveRunTimesCsvAsync(workflow, runTimes);
                StatusLabel.Text = $"Completed {runsCount} runs of workflow: {workflow}.";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Completed {runsCount} runs of workflow: {workflow}. CSV save failed: {ex.Message}";
            }
        }

        private async void OnSceneViewTapped(object? sender, Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
        {
            double tolerance = 20.0;
            int maximumResults = 1;
            bool onlyReturnPopups = false;

            try
            {
                IdentifyGraphicsOverlayResult identifyResult = await sceneView.IdentifyGraphicsOverlayAsync(
                    graphicsOverlay,
                    e.Position,
                    tolerance,
                    onlyReturnPopups,
                    maximumResults);

                if (identifyResult.Graphics.Count > 0)
                {
                    Graphic identifiedGraphic = identifyResult.Graphics.First();

                    string class_value = GetAttributeString(identifiedGraphic, "class_value");
                    string size_value = GetAttributeString(identifiedGraphic, "size_value");
                    string transparency_value = GetAttributeString(identifiedGraphic, "transparency_value");
                    string rotation_value = GetAttributeString(identifiedGraphic, "rotation_value");
                    string color_value = GetAttributeString(identifiedGraphic, "color_value");

                    StatusLabel.Text = $"Identified graphic: class_value: {class_value}, {size_value}, {transparency_value}, {rotation_value}, {color_value}";
                    ShowIdentifyEditorForGraphic(identifiedGraphic);
                }
                else
                {
                    StatusLabel.Text = "Zero graphics identified.";
                    IdentifyPopupOverlay.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error identifying graphics: {ex.Message}";
                IdentifyPopupOverlay.IsVisible = false;
            }
        }

        private static string GetAttributeString(Graphic graphic, string attributeKey)
        {
            if (graphic.Attributes.TryGetValue(attributeKey, out object? value) && value != null)
            {
                return value.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static int GetAttributeInt(Graphic graphic, string attributeKey, int defaultValue = 0)
        {
            if (graphic.Attributes.TryGetValue(attributeKey, out object? value) && value != null)
            {
                switch (value)
                {
                    case int intValue:
                        return intValue;
                    case long longValue:
                        return checked((int)longValue);
                    case string stringValue when int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed):
                        return parsed;
                    case IConvertible convertible:
                        try
                        {
                            return convertible.ToInt32(CultureInfo.InvariantCulture);
                        }
                        catch (Exception)
                        {
                            return defaultValue;
                        }
                }
            }

            return defaultValue;
        }

        private static int ParseAttributeInt(string? value, int defaultValue = 0)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return parsed;
            }

            return defaultValue;
        }

        private void ShowIdentifyEditorForGraphic(Graphic graphic)
        {
            currentlyIdentifiedGraphic = graphic;
            ClassValueEntry.Text = GetAttributeInt(graphic, "class_value").ToString(CultureInfo.InvariantCulture);
            SizeValueEntry.Text = GetAttributeInt(graphic, "size_value").ToString(CultureInfo.InvariantCulture);
            TransparencyValueEntry.Text = GetAttributeInt(graphic, "transparency_value").ToString(CultureInfo.InvariantCulture);
            RotationValueEntry.Text = GetAttributeInt(graphic, "rotation_value").ToString(CultureInfo.InvariantCulture);
            ColorValueEntry.Text = GetAttributeInt(graphic, "color_value").ToString(CultureInfo.InvariantCulture);
            IdentifyPopupOverlay.IsVisible = true;
        }

        private void OnIdentifyEditorCancelClicked(object sender, EventArgs e)
        {
            currentlyIdentifiedGraphic = null;
            IdentifyPopupOverlay.IsVisible = false;
        }

        private void OnIdentifyEditorOkClicked(object sender, EventArgs e)
        {
            if (currentlyIdentifiedGraphic == null)
            {
                IdentifyPopupOverlay.IsVisible = false;
                return;
            }

            currentlyIdentifiedGraphic.Attributes["class_value"] = ParseAttributeInt(ClassValueEntry.Text);
            currentlyIdentifiedGraphic.Attributes["size_value"] = ParseAttributeInt(SizeValueEntry.Text);
            currentlyIdentifiedGraphic.Attributes["transparency_value"] = ParseAttributeInt(TransparencyValueEntry.Text);
            currentlyIdentifiedGraphic.Attributes["rotation_value"] = ParseAttributeInt(RotationValueEntry.Text);
            currentlyIdentifiedGraphic.Attributes["color_value"] = ParseAttributeInt(ColorValueEntry.Text);

            StatusLabel.Text = "Updated identified graphic attributes.";
            IdentifyPopupOverlay.IsVisible = false;
            currentlyIdentifiedGraphic = null;
        }

    }
}