using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using Common;
using GraphX.PCL.Common.Models;
using Mono.Addins;
using QuickGraph;
using QuickGraph.GraphXAdapter;
using PluginsCommon;
using QuickGraph.Algorithms.Search;
using System;
using System.ComponentModel;
using QuickGraph.Algorithms.GraphColoring.VertexColoring;
using Microsoft.FSharp.Control;
using MessageBox = System.Windows.Forms.MessageBox;

[assembly: Addin]
[assembly: AddinDependency("GraphTasks", "1.0")]

namespace VertexColoringPlugin
{
    using BiGraph = BidirectionalGraph<GraphXVertex, GraphXTaggedEdge<GraphXVertex, int>>;
    using UnGraph = UndirectedGraph<GraphXVertex, TaggedEdge<GraphXVertex, int>>;
    using GraphArea = BidirectionalGraphArea<GraphXVertex, GraphXTaggedEdge<GraphXVertex, int>>;
    using System.Windows;
    using QuickGraph.Algorithms;
    [Extension]
    public class VertexColoringPlugin : IAlgorithm
    {
        private readonly GraphArea _graphArea;
        private readonly GraphXZoomControl _zoomControl;
        private bool _hasStarted;
        private bool _hasFinished;
        private List<Tuple<GraphXVertex, GraphXVertex, int>> states;
        private EdgeColoringAlgorithm<GraphXVertex, TaggedEdge<GraphXVertex, int>> coloringAlgorithm;
        private UndirectedGraph<GraphXVertex, TaggedEdge<GraphXVertex, int>> graphWithColoredVertices;
        private BiGraph bigraph;
        private int currStateNumber;
        private String[] colors;

        byte a = 0xFF, r = 0xE3, g = 0xE3, b = 0xE3;

        public VertexColoringPlugin()
        {
            _graphArea = new GraphArea();
            _zoomControl = new GraphXZoomControl { Content = _graphArea };
            Output.Controls.Add(new ElementHost { Dock = DockStyle.Fill, Child = _zoomControl });
        }

        public string Name => "Edge Coloring Plugin";
        public string Author => "Alex Rabochiy";
        public string Description => "This plugin demonstrates how Edge Coloring works.\n";

        public Panel Options { get; } = new Panel { Dock = DockStyle.Fill };
        public Panel Output { get; } = new Panel { Dock = DockStyle.Fill };

        public bool CanGoBack => currStateNumber != 0 && states.Count != 0;
        public bool CanGoFurther => _hasStarted && !_hasFinished;

        public void Run(string dotSource)
        {
            colors = genColors();
            var input = ReadInputGraph(dotSource);
            if (!input.Vertices.Any())
            {
                MessageBox.Show("Graph is empty.");
                return;
            }
            coloringAlgorithm = new EdgeColoringAlgorithm<GraphXVertex, TaggedEdge<GraphXVertex, int>>();

            states = new List<Tuple<GraphXVertex, GraphXVertex, int>>();

            coloringAlgorithm.Colored += OnColourEdge;
            currStateNumber = -1;

            graphWithColoredVertices = coloringAlgorithm.Compute(input);

            _graphArea.LogicCore.Graph = bigraph;
            _graphArea.GenerateGraph();
            _zoomControl.ZoomToFill();

            _hasStarted = true;
            _hasFinished = false;
        }

        private void OnColourEdge(object sender, Tuple<GraphXVertex, GraphXVertex, int> args)
        {
            states.Add(args);
        }

        private void HighlightTraversal()
        {
            var currState = states.ElementAt(currStateNumber);
            GraphXVertex source = currState.Item1;
            GraphXVertex target = currState.Item2;
            GraphXTaggedEdge<GraphXVertex, int> edge = null;
            foreach (var key in _graphArea.EdgesList.Keys)
            {
                if (key.Source.Text.Equals(source.Text) && key.Target.Text.Equals(target.Text))
                    edge = key;
            }

            var color = colors[currState.Item3];
            _graphArea.EdgesList[edge].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

            if (currStateNumber + 1 < states.Count)
            {
                var nextState = states.ElementAt(currStateNumber + 1);
                GraphXVertex nextSource = nextState.Item1;
                GraphXVertex nextTarget = nextState.Item2;
                foreach (var key in _graphArea.EdgesList.Keys)
                {
                    if (key.Source.Text.Equals(nextSource.Text) && key.Target.Text.Equals(nextTarget.Text))
                        edge = key;
                }
                _graphArea.EdgesList[edge].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF000000"));
            }
        }

        public void NextStep()
        {
            currStateNumber++;
            HighlightTraversal();
            _hasFinished = currStateNumber == states.Count - 1;
        }

        public void PreviousStep()
        {
            currStateNumber--;
            HighlightTraversal();
            _hasFinished = false;
        }

        private UndirectedGraph<GraphXVertex, TaggedEdge<GraphXVertex, int>> ReadInputGraph(string dotSource)
        {
            var vertexFun = VertexFactory.Name;
            var edgeFun1 = EdgeFactory<GraphXVertex>.WeightedTaggedEdge(0);
            var edgeFun = EdgeFactory<GraphXVertex>.Weighted(0);
            var graph = UnGraph.LoadDot(dotSource, vertexFun, edgeFun1);
            bigraph = BiGraph.LoadDot(dotSource, vertexFun, edgeFun);

            return graph;
        }

        private String[] genColors()
        {
            return new String[128] {
                "#FFFF00", "#1CE6FF", "#FF34FF", "#FF4A46", "#008941", "#006FA6", "#A30059", "#000000",
                "#FFDBE5", "#7A4900", "#0000A6", "#63FFAC", "#B79762", "#004D43", "#8FB0FF", "#997D87",
                "#5A0007", "#809693", "#FEFFE6", "#1B4400", "#4FC601", "#3B5DFF", "#4A3B53", "#FF2F80",
                "#61615A", "#BA0900", "#6B7900", "#00C2A0", "#FFAA92", "#FF90C9", "#B903AA", "#D16100",
                "#DDEFFF", "#000035", "#7B4F4B", "#A1C299", "#300018", "#0AA6D8", "#013349", "#00846F",
                "#372101", "#FFB500", "#C2FFED", "#A079BF", "#CC0744", "#C0B9B2", "#C2FF99", "#001E09",
                "#00489C", "#6F0062", "#0CBD66", "#EEC3FF", "#456D75", "#B77B68", "#7A87A1", "#788D66",
                "#885578", "#FAD09F", "#FF8A9A", "#D157A0", "#BEC459", "#456648", "#0086ED", "#886F4C",
                "#34362D", "#B4A8BD", "#00A6AA", "#452C2C", "#636375", "#A3C8C9", "#FF913F", "#938A81",
                "#575329", "#00FECF", "#B05B6F", "#8CD0FF", "#3B9700", "#04F757", "#C8A1A1", "#1E6E00",
                "#7900D7", "#A77500", "#6367A9", "#A05837", "#6B002C", "#772600", "#D790FF", "#9B9700",
                "#549E79", "#FFF69F", "#201625", "#72418F", "#BC23FF", "#99ADC0", "#3A2465", "#922329",
                "#5B4534", "#FDE8DC", "#404E55", "#0089A3", "#CB7E98", "#A4E804", "#324E72", "#6A3A4C",
                "#83AB58", "#001C1E", "#D1F7CE", "#004B28", "#C8D0F6", "#A3A489", "#806C66", "#222800",
                "#BF5650", "#E83000", "#66796D", "#DA007C", "#FF1A59", "#8ADBB4", "#1E0200", "#5B4E51",
                "#C895C5", "#320033", "#FF6832", "#66E1D3", "#CFCDAC", "#D0AC94", "#7ED379", "#012C58"
            };
        }
    }
}