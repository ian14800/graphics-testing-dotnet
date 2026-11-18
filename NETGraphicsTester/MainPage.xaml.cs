using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Location;
using ArcLocation = Esri.ArcGISRuntime.Location.Location;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.Maui;
using System.Reflection.Metadata;

namespace NETGraphicsTester
{
    public partial class MainPage : ContentPage
    {
        Random random = new Random();
        Graphic? currentlyIdentifiedGraphic;

        string passiveMode = "normal";
        int graphicsCount = 0;

        private bool _sceneReady;
        private readonly LocationDataSource _locationSource = new SystemLocationDataSource();

        Surface baseSurface;

        GraphicsOverlay graphicsOverlay = new GraphicsOverlay();
        SimpleRenderer defaultRenderer = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Yellow, 15));

        public MainPage()
        {
            InitializeComponent();
            _ = InitializeSceneAsync();
        }

        private void SceneView_DrawStatusChanged(object? sender, EventArgs e)
        {
            if (sceneView.DrawStatus == DrawStatus.Completed)
            {
                Debug.WriteLine("Draw completed.");
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
            try
            {
                var localSceneView = sceneView;
                if (localSceneView == null)
                {
                    Debug.WriteLine("Scene view is not ready.");
                    return;
                }

                localSceneView.WarningsChanged += SceneView_WarningsChanged;

                var scene = new Scene(SceneViewingMode.Local, BasemapStyle.ArcGISChartedTerritory);
                var camera = new Camera(37.7, -122.4194, 15000, 0, 30, 0);


                string _elevationServiceUrl = "https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";
                baseSurface = new Surface();
                ArcGISTiledElevationSource elevationSource = new ArcGISTiledElevationSource(new Uri(_elevationServiceUrl));
                baseSurface.ElevationSources.Add(elevationSource);
                scene.BaseSurface = baseSurface;

                await scene.LoadAsync();

                scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
                graphicsOverlay.Renderer = defaultRenderer;
                localSceneView.GraphicsOverlays?.Add(graphicsOverlay);

                localSceneView.Scene = scene;

                localSceneView.SetViewpointCamera(camera);
                localSceneView.DrawStatusChanged += SceneView_DrawStatusChanged;
                localSceneView.GeoViewTapped += OnSceneViewTapped_Default;

                _sceneReady = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error thrown: {ex.Message}");
            }
        }

        async private Task AddGraphicsOperation(Geometry geometry)
        {
            Graphic graphic = new Graphic(geometry);
            graphicsOverlay.Graphics.Add(graphic);
        }

        private async Task AddRandomGraphics(int count)
        {
            Envelope? extent = sceneView.GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry as Envelope;
            MapPoint? center = sceneView.GetCurrentViewpoint(ViewpointType.CenterAndScale)?.TargetGeometry as MapPoint;
            double baseZ = center?.Z ?? 0;
            const double zRange = 500;

            if (extent == null || center == null)
            {
                Debug.WriteLine("Extent or center is null");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                double x = extent.XMin + (random.NextDouble() * (extent.XMax - extent.XMin));
                double y = extent.YMin + (random.NextDouble() * (extent.YMax - extent.YMin));
                double z = baseZ + ((random.NextDouble() * 2 * zRange) - zRange);
                MapPoint point = new MapPoint(x, y, z, extent.SpatialReference);

                Graphic graphic = new Graphic(point);
                graphicsOverlay.Graphics.Add(graphic);
            }
        }
        private void RemoveGraphicsOperation(int? count = null, int? index = null)
        {
            if (count.HasValue)
            {
                int removeCount = Math.Min(count.Value, graphicsOverlay.Graphics.Count);
                for (int i = 0; i < removeCount; i++)
                {
                    graphicsOverlay.Graphics.RemoveAt(graphicsOverlay.Graphics.Count - 1);
                }
            }
            if (index.HasValue)
            {
                if (index.Value >= 0 && index.Value < graphicsOverlay.Graphics.Count)
                {
                    graphicsOverlay.Graphics.RemoveAt(index.Value);
                }
            }
        }


        async private Task<MultilayerPointSymbol> createModelLayerFromFile(string fileName, int size = 50)
        {
            string file = $"{fileName}.glb";

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


        private void MoveGraphics(int index, Geometry newGeometry)
        {
            if (index < 0 || index >= graphicsOverlay.Graphics.Count)
            {
                return;
            }

            Graphic graphic = graphicsOverlay.Graphics[index];
            graphic.Geometry = newGeometry;
        }

        private void ToggleGraphicsVisibility(int index)
        {
            if (graphicsOverlay.Graphics[index].IsVisible)
            {
                graphicsOverlay.Graphics[index].IsVisible = false;
            } else
            {
                graphicsOverlay.Graphics[index].IsVisible = true;
            }
        }

        private async void OnSceneViewTapped_Default(object? sender, Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
        {
            Debug.WriteLine($"Scene view tapped at position: {e.Position}");
        }

        private bool ResetAndClear()
        {
            if (!_sceneReady)
            {
                Debug.WriteLine("Cannot reset graphics: the scene is not ready.");
                return false;
            }

            var overlays = sceneView.GraphicsOverlays;
            if (overlays is null)
            {
                Debug.WriteLine("Cannot reset graphics: overlays are unavailable.");
                return false;
            }

            sceneView.GeoViewTapped -= OnSceneViewTapped_AddGraphics;
            sceneView.GeoViewTapped -= OnSceneViewTapped_IdentifyOn;

            graphicsOverlay.Graphics.Clear();

            var replacement = new GraphicsOverlay
            {
                Renderer = new SimpleRenderer(
                    new SimpleMarkerSymbol(
                        SimpleMarkerSymbolStyle.Circle,
                        System.Drawing.Color.Yellow,
                        15))
            };

            overlays.Clear();
            graphicsOverlay = replacement;
            overlays.Add(graphicsOverlay);

            _locationSource.LocationChanged -= OnLocationChanged;
            _locationSource.StopAsync();

            graphicsCount = 0;
            passiveMode = "normal";

            return true;
        }

        private async void ExecuteWorkflow_ModelLODs(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 1] Use graphic to temporarily preview one LOD of a 3d model");

            passiveMode = "modelLODs";
            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_SearchResults(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 2] Drawing of points, search results and table clicking");
        }

        private async void ExecuteWorkflow_GPSAccuracy(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 3] Topmost accuracy layer to compare with GPS readings (GPS readings overlay)");
        }

        private async void ExecuteWorkflow_DataExtraction(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 4] Graphics as extraction points for underlying data");
            passiveMode = "dataExtraction";

            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_EditingExport(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 5] Graphics as editing/sketch workflow, export of features");
            passiveMode = "editingExport";
            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_SelectionHighlight(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[JS WORKFLOW 6] Selection highlight as separate overlay, using a feature table as source");
            passiveMode = "selectionHighlight";

            await ImportGraphicsFromServiceAsync("https://services5.arcgis.com/N82JbI5EYtAkuUKU/arcgis/rest/services/SF_Points_3D/FeatureServer/12");

            // Turn on highlighting behavior
            sceneView.GeoViewTapped += OnSceneViewTapped_IdentifyOn;
        }

        private async void ExecuteWorkflow_MissileTrajectory(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[.NET WORKFLOW 1] Choose two points as calc input, show missile trajectory");
            passiveMode = "missileTrajectory";
            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_WebRenderer(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[.NET WORKFLOW 2] Class based + size based renderer from web fetched json");

            await ReadGeoJsonPoints("SF_points.geojson");

            await ApplySymbols("webStyles");
        }

        private async void ExecuteWorkflow_CreateTrack(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[.NET WORKFLOW 3] Create track (GPX) from location and search for POI from service (open street map?)");
        }

        private async void ExecuteWorkflow_FlashingUpdates(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[.NET WORKFLOW 4] Flashing graphics for updated data, polling rest endpoint");
        }

        private async void ExecuteWorkflow_PicturePopups(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[FLUTTER WORKFLOW 1] Embedded json with pictures for popups");
        }

        private async void ExecuteWorkflow_GeocodePoints(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[FLUTTER WORKFLOW 2] Geocoded points or taps");

            passiveMode = "geocode";
            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_TransferOverlay(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[FLUTTER WORKFLOW 3] Transfer overlay between map/scene");

            await ImportGraphicsFromServiceAsync("https://services5.arcgis.com/N82JbI5EYtAkuUKU/arcgis/rest/services/SF_Points_3D/FeatureServer/12");
            await TransferOverlayToMap();
        }

        private async void ExecuteWorkflow_QueryByExtent(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[FLUTTER WORKFLOW 4] Query by extent and draw data");

            await ImportGraphicsFromServiceAsync("https://services5.arcgis.com/N82JbI5EYtAkuUKU/arcgis/rest/services/SF_Points_3D/FeatureServer/12", "extent");
        }

        private async void ExecuteWorkflow_FilterGraphics(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[FLUTTER WORKFLOW 5] Load graphics from feature service, apply filters by search");

            await ImportGraphicsFromServiceAsync("https://services5.arcgis.com/N82JbI5EYtAkuUKU/arcgis/rest/services/SF_Points_3D/FeatureServer/12");

            string jsonString = File.ReadAllText("unique_value_renderer.json");
            Renderer uniqueValueRenderer = Renderer.FromJson(jsonString);
            graphicsOverlay.Renderer = uniqueValueRenderer;

            Thread.Sleep(3000);

            Debug.WriteLine("Applying filter for graphics of type 1");
            await ImportGraphicsFromServiceAsync("https://services5.arcgis.com/N82JbI5EYtAkuUKU/arcgis/rest/services/SF_Points_3D/FeatureServer/12", "class_value = 1");
        }

        private async void ExecuteWorkflow_POIByRoute(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[FLUTTER WORKFLOW 6] POI source displayed by selected route");
        }

        private async void ExecuteWorkflow_MultifacetedRenderer(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[QT WORKFLOW 1] Complex multifaceted renderer based on selection location");

            await ReadGeoJsonPoints("SF_points.geojson");

            await ApplySymbols("multifaceted");
        }

        private async void ExecuteWorkflow_GeoJSONGraphics(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[QT WORKFLOW 2] Graphics from geojson");

            await ReadGeoJsonPoints("SF_points.geojson");
        }

        private async void ExecuteWorkflow_TraverseRoute(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[QT WORKFLOW 3] Route pins, switch to traversed based on location traveled.");
        }

        private async void ExecuteWorkflow_ComplexPopup(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[QT WORKFLOW 4] Graphic identify and complex popup with embedded images");
            await ReadJsonPoints("SF_Park_Points.json");
            passiveMode = "complexPopup";
            sceneView.GeoViewTapped += OnSceneViewTapped_IdentifyOn;
        }

        private async void ExecuteWorkflow_TextLabels(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[QT WORKFLOW 5] Labels as text symbols");

            await ReadGeoJsonPoints("SF_points.geojson");
            await ApplySymbols("text");
            await ReadGeoJsonPoints("SF_points.geojson");
        }

        private async void ExecuteWorkflow_FollowGraphic(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[QT WORKFLOW 6] Following a graphic");

            MapPoint startPoint = new MapPoint(-13634471.9113, 4550391.4384, SpatialReferences.WebMercator);
            graphicsOverlay.Graphics.Add(new Graphic(startPoint));

            MapPoint endPoint = new MapPoint(-13628102.2396, 4548508.0037, SpatialReferences.WebMercator);
            graphicsOverlay.Graphics.Add(new Graphic(endPoint));

            createMissileTrajectory();

            SimpleMarkerSymbol followSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Pink, 30);
            Graphic followGraphic = new Graphic(startPoint, followSymbol);

            graphicsOverlay.Graphics.Add(followGraphic);

            var trackGraphics = sceneView.GraphicsOverlays.ElementAt(1).Graphics;

            foreach (var graphic in trackGraphics)
            {
                var geometry = graphic.Geometry;
                if (geometry is MapPoint mapPoint)
                {
                    followGraphic.Geometry = mapPoint;
                    sceneView.SetViewpoint(new Viewpoint(mapPoint));
                }
                Thread.Sleep(10);
            }
        }

        private async void ExecuteWorkflow_ShowNearbyPoints(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[SWIFT WORKFLOW 1] Show points nearby location, how should graphics be stored?");
        }

        private async void ExecuteWorkflow_DroneTrack(object? sender, EventArgs e)
        {
            if (!ResetAndClear()) return;
            Debug.WriteLine("[SWIFT WORKFLOW 2] Drone flying by track with location, tap to move there");
        }

        private async void OnSceneViewTapped_AddGraphics(object sender, Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
        {
            if (e.Location is not MapPoint tappedLocation)
            {
                Debug.WriteLine("Tap did not return a valid location");
                return;
            }
            Graphic graphic = new Graphic(tappedLocation);

            graphicsOverlay.Graphics.Add(graphic);
            graphicsCount++;

            switch(passiveMode)
            {
                case "editingExport":
                    if (graphicsCount > 4)
                    {
                        Debug.WriteLine("Maximum graphics count reached.");
                        sceneView.GeoViewTapped -= OnSceneViewTapped_AddGraphics;
                        ExportGraphics();

                        return;
                    }
                    break;
                case "missileTrajectory":
                    if (graphicsCount == 2)
                    {
                        Debug.WriteLine("Maximum graphics count reached.");
                        sceneView.GeoViewTapped -= OnSceneViewTapped_AddGraphics;
                        createMissileTrajectory();

                        return;
                    }
                    break;
                case "dataExtraction":
                    double elevation = await baseSurface.GetElevationAsync(graphic.Geometry as MapPoint);
                    sceneView.GeoViewTapped -= OnSceneViewTapped_AddGraphics;

                    var textSymbol = new TextSymbol(elevation.ToString(), System.Drawing.Color.Red, 12, Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Left, Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom);
                    graphicsOverlay.Graphics.Add(new Graphic(graphic.Geometry, textSymbol));

                    Debug.WriteLine($"Elevation at tapped location: {elevation}");
                    break;
                case "geocode":
                    string address = await GetAddressAsync(tappedLocation);
                    var addressLabel = new TextSymbol(address, System.Drawing.Color.Red, 12, Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Left, Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom);
                    Graphic addressGraphic = new Graphic(tappedLocation, addressLabel);
                    graphicsOverlay.Graphics.Add(addressGraphic);
                    break;
                case "modelLODs":
                    if (graphicsCount > 2)
                    {
                        await ApplySymbols("modelLODs");
                        sceneView.GeoViewTapped -= OnSceneViewTapped_AddGraphics;
                    }
                    break;
                default:
                    break;
            }
        }

        private async void OnSceneViewTapped_IdentifyOn(object sender, Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
        {
            double tolerance = 10.0;
            int maximumResults = 5;
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
                    var attributes = identifiedGraphic.Attributes;

                    switch (passiveMode)
                    {
                        case "selectionHighlight":
                            GraphicsOverlay highlightOverlay = new GraphicsOverlay();
                            SimpleRenderer highlightRenderer = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Cyan, 10));
                            highlightOverlay.Renderer = highlightRenderer;
                            Graphic highlightedGraphic = new Graphic(identifiedGraphic.Geometry);

                            highlightOverlay.Graphics.Add(highlightedGraphic);
                            sceneView.GraphicsOverlays.Add(highlightOverlay);
                            break;
                        case "complexPopup":
                            string parkName = attributes.ContainsKey("Name") ? attributes["Name"].ToString() : "Unknown park";
                            string parkDescription = attributes.ContainsKey("Description") ? attributes["Description"].ToString() : "No description available";

                            var content = new VerticalStackLayout
                            {
                                Padding = 20,
                                Spacing = 12
                            };

                            content.Children.Add(new Label
                            {
                                Text = parkName,
                                FontSize = 24,
                                FontAttributes = FontAttributes.Bold
                            });

                            content.Children.Add(new Label { Text = parkDescription });

                            if (attributes.TryGetValue("Picture", out var picture) && picture is string dataUri && !string.IsNullOrWhiteSpace(dataUri))
                            {
                                int comma = dataUri.IndexOf(',');
                                if (comma < 0 ||
                                            !dataUri.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ||
                                            !dataUri.Substring(0, comma).EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new FormatException("Park picture is not a base64 image data URI.");
                                }

                                byte[] imageBytes = Convert.FromBase64String(dataUri.Substring(comma + 1));
                                content.Children.Add(new Image
                                {
                                    Source = ImageSource.FromStream(() => new MemoryStream(imageBytes)),
                                    HeightRequest = 250,
                                    Aspect = Aspect.AspectFit
                                });
                            }
                            else
                            {
                                throw new FormatException("Park picture is missing or empty.");
                            }

                            var closeButton = new Button { Text = "Close" };
                            closeButton.Clicked += async (_, _) => await Navigation.PopModalAsync();
                            content.Children.Add(closeButton);

                            var popupPage = new ContentPage
                            {
                                Title = parkName,
                                Content = new ScrollView { Content = content }
                            };

                            await Navigation.PushModalAsync(popupPage);                        
                            break;
                        default:
                            identifiedGraphic.IsSelected = !identifiedGraphic.IsSelected;
                            break;
                    }
                }
                else
                {
                    Debug.WriteLine("Zero graphics identified.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error identifying graphics: {ex.Message}");
            }
        }

        private async Task ApplySymbols(string symbolType)
        {
            List<Graphic> graphics = graphicsOverlay.Graphics.ToList();

            foreach (var graphic in graphics)
            {
                var attributes = graphic.Attributes;

                if (symbolType == "multifaceted")
                {
                    SimpleMarkerSymbol highSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Green, 20);
                    SimpleMarkerSymbol mediumSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Yellow, 15);
                    SimpleMarkerSymbol lowSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 10);

                    if (Convert.ToInt32(attributes["class_value"]) > 3 && Convert.ToInt32(attributes["att_value"]) > 80)
                    {
                        graphic.Symbol = highSymbol;
                    }
                    else if (Convert.ToInt32(attributes["class_value"]) > 1 && Convert.ToInt32(attributes["att_value"]) > 40)
                    {
                        graphic.Symbol = mediumSymbol;
                    }
                    else
                    {
                        graphic.Symbol = lowSymbol;
                    }
                }
                else if (symbolType == "text")
                {
                    string classValue = attributes["class_value"].ToString();
                    string textLabel;

                    switch (classValue)
                    {
                        case "1":
                            textLabel = "one";
                            break;
                        case "2":
                            textLabel = "two";
                            break;
                        case "3":
                            textLabel = "three";
                            break;
                        case "4":
                            textLabel = "four";
                            break;
                        case "5":
                            textLabel = "five";
                            break;
                        default:
                            textLabel = "unknown";
                            break;
                    }
                    graphic.Symbol = new TextSymbol(textLabel, System.Drawing.Color.Black, 12, Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Right, Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom);
                }
                else if (symbolType == "webStyles")
                {
                    int classValue = Convert.ToInt32(attributes["class_value"]);
                    int attValue = Convert.ToInt32(attributes["att_value"]);

                    var stylesByName = new Dictionary<string, SymbolStyle>();
                    ArcGISPortal portal = await ArcGISPortal.CreateAsync();

                    PortalItem styleItem = await PortalItem.CreateAsync(portal, "220936cc6ed342c9937abd8f180e7d1e");
                    var style = await SymbolStyle.OpenAsync(styleItem);
                    Symbol symbol;

                    switch (classValue)
                    {
                        case 1:
                            symbol = await style.GetSymbolAsync(new[] { "airport" });
                            break;
                        case 2:
                            symbol = await style.GetSymbolAsync(new[] { "amusement-park" });
                            break;
                        case 3:
                            symbol = await style.GetSymbolAsync(new[] { "aquarium" });
                            break;
                        case 4:
                            symbol = await style.GetSymbolAsync(new[] { "restaurant" });
                            break;
                        case 5:
                            symbol = await style.GetSymbolAsync(new[] { "hotel" });
                            break;
                        default:
                            symbol = await style.GetSymbolAsync(new[] { "pharmacy" });
                            break;
                    }

                    if (symbol is MultilayerPointSymbol multilayerPointSymbol)
                    {
                        multilayerPointSymbol.Size = attValue;
                        graphic.Symbol = multilayerPointSymbol;
                    }
                    else
                    {
                        Debug.WriteLine("Symbol is not a MultilayerPointSymbol.");
                    }
                }
                else if (symbolType == "modelLODs")
                {
                    MultilayerPointSymbol modelSymbollow = await createModelLayerFromFile("spider_low");
                    MultilayerPointSymbol modelSymbolmedium = await createModelLayerFromFile("spider_medium");
                    MultilayerPointSymbol modelSymbolhigh = await createModelLayerFromFile("spider_high");

                    graphicsOverlay.Graphics[0].Symbol = modelSymbollow;
                    graphicsOverlay.Graphics[1].Symbol = modelSymbolmedium;
                    graphicsOverlay.Graphics[2].Symbol = modelSymbolhigh;
                }
            }
        }

        private async void StartLocationTracking()
        {
            _locationSource.LocationChanged += OnLocationChanged;
            await _locationSource.StartAsync();
        }

        private void OnLocationChanged(object? sender, ArcLocation location)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Debug.WriteLine($"Location changed: {location.Position}");
            });
        }

        private void ExportGraphics()
        {
            Debug.WriteLine("Exporting graphics...");
            foreach (var graphic in graphicsOverlay.Graphics)
            {
                Debug.WriteLine($"Graphic: {graphic.Geometry}");
            }
        }

        private void createMissileTrajectory()
        {
            Debug.WriteLine("Creating missile trajectory...");

            if (!(graphicsOverlay.Graphics.Count == 2))
            {
                Debug.WriteLine("Incorrect number of graphics for missile trajectory.");
                return;
            }

            Graphic startGraphic = graphicsOverlay.Graphics[0];
            Graphic endGraphic = graphicsOverlay.Graphics[1];

            MapPoint startPoint = (MapPoint)startGraphic.Geometry;
            MapPoint endPoint = (MapPoint)endGraphic.Geometry;

            GraphicsOverlay trajectoryOverlay = new GraphicsOverlay();
            SimpleRenderer simpleRenderer = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Red, 2));
            trajectoryOverlay.Renderer = simpleRenderer;

            for (int i = 0; i < 1000; i ++)
            {
                double dx = startPoint.X - endPoint.X;
                double dy = startPoint.Y - endPoint.Y;
                double dz = startPoint.Z - endPoint.Z;

                MapPoint trajectoryPoint = new MapPoint(
                    startPoint.X - dx * i / 1000,
                    startPoint.Y - dy * i / 1000,
                    startPoint.Z - dz * i / 1000,
                    startPoint.SpatialReference
                );
                Graphic trajectoryGraphic = new Graphic(trajectoryPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 2));
                trajectoryOverlay.Graphics.Add(trajectoryGraphic);
            }

            sceneView.GraphicsOverlays.Add(trajectoryOverlay);
        }

        private async Task ReadGeoJsonPoints(string json)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(json);
            using var reader = new StreamReader(stream);

            string fileContent = await reader.ReadToEndAsync();

            using var document = JsonDocument.Parse(fileContent);
            var root = document.RootElement;

            if (root.GetProperty("type").GetString() != "FeatureCollection")
                throw new NotSupportedException("Expected a GeoJSON FeatureCollection");

            var graphics = new List<Graphic>();

            foreach (var feature in root.GetProperty("features").EnumerateArray())
            {
                var geometry = feature.GetProperty("geometry");
            
                if (geometry.ValueKind == JsonValueKind.Null || geometry.GetProperty("type").GetString() != "Point")
                {
                    throw new NotSupportedException("Encountered non-Point geometry");
                }

                var coordinates = geometry.GetProperty("coordinates");

                var point = coordinates.GetArrayLength() > 2
                    ? new MapPoint(
                        coordinates[0].GetDouble(),
                        coordinates[1].GetDouble(),
                        coordinates[2].GetDouble(),
                        SpatialReferences.WebMercator
                    )
                    : new MapPoint(
                        coordinates[0].GetDouble(),
                        coordinates[1].GetDouble(),
                        SpatialReferences.WebMercator
                    );

                Graphic graphic = new Graphic(point);

                if (feature.TryGetProperty("properties", out var properties) &&
                    properties.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in properties.EnumerateObject())
                    {
                        graphic.Attributes[property.Name] = property.Value.ToString();
                    }
                }

                graphics.Add(graphic);
            }

            foreach(var graphic in graphics)
            {
                graphicsOverlay.Graphics.Add(graphic);
            }
        }

        private async Task ReadJsonPoints(string json)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(json);
            using var reader = new StreamReader(stream);

            string fileContent = await reader.ReadToEndAsync();

            using var document = JsonDocument.Parse(fileContent);
            var root = document.RootElement;

            var graphics = new List<Graphic>();

            foreach (var feature in root.GetProperty("features").EnumerateArray())
            {
                var x = feature.GetProperty("geometry").GetProperty("x").GetDouble();
                var y = feature.GetProperty("geometry").GetProperty("y").GetDouble();

                var point = new MapPoint(x, y, SpatialReferences.WebMercator);

                Graphic graphic = new Graphic(point);

                if (feature.TryGetProperty("attributes", out var attributes) &&
                    attributes.ValueKind == JsonValueKind.Object)
                {
                    foreach (var attribute in attributes.EnumerateObject())
                    {
                        graphic.Attributes[attribute.Name] = attribute.Value.ToString();
                    }
                }
                graphics.Add(graphic);
            }
            foreach(var graphic in graphics)
            {
                graphicsOverlay.Graphics.Add(graphic);
            }
        }

        private async Task ImportGraphicsFromServiceAsync(string serviceUrl, string whereClause = "1=1")
        {
            try
            {
                Uri uri = new Uri(serviceUrl);

                var table = new ServiceFeatureTable(uri);
                await table.LoadAsync();

                if (whereClause == "extent")
                {
                    var extent = sceneView.GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry?? throw new InvalidOperationException("Scene extent unavailable");

                    var query = new QueryParameters
                    {
                        WhereClause = "1=1",
                        MaxFeatures = 500,
                        Geometry = extent,
                        SpatialRelationship = SpatialRelationship.Intersects
                    };

                    var result = await table.QueryFeaturesAsync(query, QueryFeatureFields.LoadAll);

                    foreach (var feature in result)
                    {
                        SimpleMarkerSymbol symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Black, 10);
                        graphicsOverlay.Graphics.Add(new Graphic(feature.Geometry, feature.Attributes, symbol));
                    }
                }
                else
                {
                    var query = new QueryParameters
                    {
                        WhereClause = whereClause,
                        MaxFeatures = 500
                    };

                    var result = await table.QueryFeaturesAsync(query, QueryFeatureFields.LoadAll);

                    foreach (var feature in result)
                    {
                        graphicsOverlay.Graphics.Add(new Graphic(feature.Geometry, feature.Attributes));
                    }     
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        private async Task<string> GetAddressAsync(MapPoint point)
        {
                var locator = await LocatorTask.CreateAsync(new Uri(
                    "https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer"));

                var results = await locator.ReverseGeocodeAsync(
                    point,
                    new ReverseGeocodeParameters
                    {
                        MaxResults = 1
                    });

                return results.FirstOrDefault()?.Label ?? "No address found";
        }

        private async Task TransferOverlayToMap()
        {
            var mapView = new MapView
            {
                Map = new Esri.ArcGISRuntime.Mapping.Map(BasemapStyle.ArcGISImagery)
            };

            sceneView.GraphicsOverlays.Remove(graphicsOverlay);
            mapView.GraphicsOverlays.Add(graphicsOverlay);

            var layout = new Microsoft.Maui.Controls.Grid
            {
                RowDefinitions =
                {
                    new RowDefinition {Height = GridLength.Auto},
                    new RowDefinition {Height = GridLength.Star}
                }
            };

            layout.Add(mapView, 0, 1);

            var page = new ContentPage
            {
                Title = "Map view",
                Content = layout
            };

            await Navigation.PushAsync(page);
        }
    }
}