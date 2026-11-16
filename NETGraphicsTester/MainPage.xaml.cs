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
        Random random = new Random();
        Graphic? currentlyIdentifiedGraphic;

        string passiveMode = "normal";
        int graphicsCount = 0;

        private bool _sceneReady;

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
                Surface elevationSurface = new Surface();
                ArcGISTiledElevationSource elevationSource = new ArcGISTiledElevationSource(new Uri(_elevationServiceUrl));
                elevationSurface.ElevationSources.Add(elevationSource);
                scene.BaseSurface = elevationSurface;

                await scene.LoadAsync();

                scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
                graphicsOverlay.Renderer = defaultRenderer;
                localSceneView.GraphicsOverlays?.Add(graphicsOverlay);

                localSceneView.Scene = scene;

                localSceneView.SetViewpointCamera(camera);
                localSceneView.DrawStatusChanged += SceneView_DrawStatusChanged;
                localSceneView.GeoViewTapped += OnSceneViewTapped_Default;
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

            sceneView.GraphicsOverlays.Remove(graphicsOverlay);
            graphicsOverlay = replacement;
            sceneView.GraphicsOverlays.Add(graphicsOverlay);

            graphicsCount = 0;
            passiveMode = "normal";

            return true;
        }

        private async void ExecuteWorkflow_ModelLODs(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[JS WORKFLOW 1] Use graphic to temporarily preview one LOD of a 3d model");
        }

        private async void ExecuteWorkflow_SearchResults(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[JS WORKFLOW 2] Drawing of points, search results and table clicking");
        }

        private async void ExecuteWorkflow_GPSAccuracy(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[JS WORKFLOW 3] Topmost accuracy layer to compare with GPS readings (GPS readings overlay)");
        }

        private async void ExecuteWorkflow_DataExtraction(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[JS WORKFLOW 4] Graphics as extraction points for underlying data");
        }

        private async void ExecuteWorkflow_EditingExport(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[JS WORKFLOW 5] Graphics as editing/sketch workflow, export of features");
            passiveMode = "editingExport";
            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_SelectionHighlight(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[JS WORKFLOW 6] Selection highlight as separate overlay, using a feature table as source");
            passiveMode = "selectionHighlight";

            Envelope? extent = sceneView.GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry as Envelope;
            MapPoint? center = sceneView.GetCurrentViewpoint(ViewpointType.CenterAndScale)?.TargetGeometry as MapPoint;
            double baseZ = center?.Z ?? 0;
            const double zRange = 500;

            if (extent == null || center == null)
            {
                Debug.WriteLine("Extent or center is null");
                return;
            }
            // Add 10 random points
            for (int i = 0; i < 10; i++)
            {
                double x = extent.XMin + (random.NextDouble() * (extent.XMax - extent.XMin));
                double y = extent.YMin + (random.NextDouble() * (extent.YMax - extent.YMin));
                double z = baseZ + ((random.NextDouble() * 2 * zRange) - zRange);
                MapPoint point = new MapPoint(x, y, z, extent.SpatialReference);

                Graphic graphic = new Graphic(point);
                graphicsOverlay.Graphics.Add(graphic);
            }

            // Turn on highlighting behavior
            sceneView.GeoViewTapped += OnSceneViewTapped_IdentifyOn;
        }

        private async void ExecuteWorkflow_MissileTrajectory(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[.NET WORKFLOW 1] Choose two points as calc input, show missile trajectory");
            passiveMode = "missileTrajectory";
            sceneView.GeoViewTapped += OnSceneViewTapped_AddGraphics;
        }

        private async void ExecuteWorkflow_WebRenderer(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[.NET WORKFLOW 2] Class based + icon based renderers from web fetched json");
        }

        private async void ExecuteWorkflow_CreateTrack(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[.NET WORKFLOW 3] Create track (GPX) from location and search for POI from service (open street map?)");
        }

        private async void ExecuteWorkflow_FlashingUpdates(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[.NET WORKFLOW 4] Flashing graphics for updated data, polling rest endpoint");
        }

        private async void ExecuteWorkflow_PicturePopups(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[FLUTTER WORKFLOW 1] Embedded json with pictures for popups");
        }

        private async void ExecuteWorkflow_GeocodePoints(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[FLUTTER WORKFLOW 2] Geocoded points or taps");
        }

        private async void ExecuteWorkflow_TransferOverlay(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[FLUTTER WORKFLOW 3] Transfer overlay between map/scene");
        }

        private async void ExecuteWorkflow_QueryByExtent(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[FLUTTER WORKFLOW 4] Query by extent and draw data");
        }

        private async void ExecuteWorkflow_FilterGraphics(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[FLUTTER WORKFLOW 5] Load graphics from JSON and feature service, apply filters by search");
        }

        private async void ExecuteWorkflow_POIByRoute(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[FLUTTER WORKFLOW 6] POI source displayed by selected route");
        }

        private async void ExecuteWorkflow_MultifacetedRenderer(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[QT WORKFLOW 1] Complex multifaceted renderer based on selection location");
        }

        private async void ExecuteWorkflow_GeoJSONGraphics(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[QT WORKFLOW 2] Graphics from geojson");
        }

        private async void ExecuteWorkflow_TraverseRoute(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[QT WORKFLOW 3] Route pins, switch to traversed based on location traveled.");
        }

        private async void ExecuteWorkflow_ComplexPopup(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[QT WORKFLOW 4] Need graphic identify and complex popup");
        }

        private async void ExecuteWorkflow_TextLabels(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[QT WORKFLOW 5] Labels as text symbols");
        }

        private async void ExecuteWorkflow_FollowGraphic(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[QT WORKFLOW 6] Following a graphic");
        }

        private async void ExecuteWorkflow_ShowNearbyPoints(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[SWIFT WORKFLOW 1] Show points nearby location, how should graphics be stored?");
        }

        private async void ExecuteWorkflow_DroneTrack(object? sender, EventArgs e)
        {
            ResetAndClear();
            Debug.WriteLine("[SWIFT WORKFLOW 2] Drone flying by track with location, tap to move there");
        }

        private void OnSceneViewTapped_AddGraphics(object sender, Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
        {
            var tappedLocation = e.Location;
            Debug.WriteLine($"Tapped location: {tappedLocation}");

            Geometry geometry = new MapPoint(tappedLocation.X, tappedLocation.Y, tappedLocation.Z);
            Graphic graphic = new Graphic(geometry);

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
                case "MissileTrajectory":
                    if (graphicsCount == 2)
                    {
                        Debug.WriteLine("Maximum graphics count reached.");
                        sceneView.GeoViewTapped -= OnSceneViewTapped_AddGraphics;
                        createMissileTrajectory();

                        return;
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

                    switch (passiveMode)
                    {
                        case "selectionHighlight":
                            GraphicsOverlay highlightOverlay = new GraphicsOverlay();
                            SimpleRenderer highlightRenderer = new SimpleRenderer(new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Cyan, 6));
                            highlightOverlay.Renderer = highlightRenderer;
                            Graphic highlightedGraphic = new Graphic(identifiedGraphic.Geometry);

                            highlightOverlay.Graphics.Add(highlightedGraphic);
                            sceneView.GraphicsOverlays.Add(highlightOverlay);
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
    }
}