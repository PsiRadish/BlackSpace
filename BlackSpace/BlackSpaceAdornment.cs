//------------------------------------------------------------------------------
// <copyright file="BlackSpaceAdornment.cs" company="Kory Postma">
//
//   Copyright 2016-2017 Kory Postma
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System.ComponentModel.Composition;

namespace BlackSpace
{
    /// <summary>
    /// TextAdornment1 places boxes around all end-of-line whitespace in the editor window
    /// </summary>
    internal sealed class BlackSpaceAdornment
    {
        /// <summary>
        /// The layer of the adornment.
        /// </summary>
        private readonly IAdornmentLayer _layer;

        /// <summary>
        /// Text view where the adornment is created.
        /// </summary>
        private readonly IWpfTextView _view;

        /// <summary>
        /// Adornment brushs.
        /// </summary>
        private Brush _spacesBrush;
        private Pen _spacesPen;
        private Brush _tabsBrush;
        private Pen _tabsPen;

        private Brush GetBrushForChar(char ch)
        {
            switch (ch)
            {
                case ' ':
                    return _spacesBrush;
                case '\t':
                    return _tabsBrush;
                default:
                    throw new ArgumentException($"No brush for character '{ch}'");
            }
        }
        private Pen GetPenForChar(char ch)
        {
            switch (ch)
            {
                case ' ':
                    return _spacesPen;
                case '\t':
                    return _tabsPen;
                default:
                    throw new ArgumentException($"No pen for character '{ch}'");
            }
        }

        public static BlackSpaceOptionsPackage Package
        {
            get;
            private set;
        }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="inPackage">Owner package, not null.</param>
        public static void Initialize(BlackSpaceOptionsPackage inPackage)
        {
            Package = inPackage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackSpaceAdornment"/> class.
        /// </summary>
        /// <param name="view">Text view to create the adornment for</param>
        public BlackSpaceAdornment(IWpfTextView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            this._layer = view.GetAdornmentLayer("BlackSpaceTextAdornment");

            this._view = view;
            this._view.LayoutChanged += this.OnLayoutChanged;

            if (Package != null)
            {
                //This will also update all of the brushes
                Package.RegisterAdornment(this);
            }
            else
            {
                // Create the pen and brush to color the boxes around the end-of-line whitespace
                _spacesBrush = new SolidColorBrush(Constants.DefaultSpacesBackgroundColor.ToMediaColor());
                _spacesBrush.Freeze();
                var spacesPenBrush = new SolidColorBrush(Constants.DefaultSpacesBorderColor.ToMediaColor());
                spacesPenBrush.Freeze();
                _spacesPen = new Pen(spacesPenBrush, 1.0);
                _spacesPen.Freeze();

                _tabsBrush = new SolidColorBrush(Constants.DefaultTabsBackgroundColor.ToMediaColor());
                _tabsBrush.Freeze();
                var tabsPenBrush = new SolidColorBrush(Constants.DefaultTabsBorderColor.ToMediaColor());
                tabsPenBrush.Freeze();
                _tabsPen = new Pen(tabsPenBrush, 1.0);
                _tabsPen.Freeze();
            }
        }

        /// <summary>
        /// Handles whenever the text displayed in the view changes by adding the adornment to any reformatted lines
        /// </summary>
        /// <remarks><para>This event is raised whenever the rendered text displayed in the <see cref="ITextView"/> changes.</para>
        /// <para>It is raised whenever the view does a layout (which happens when DisplayTextLineContainingBufferPosition is called or in response to text or classification changes).</para>
        /// <para>It is also raised whenever the view scrolls horizontally or when its size changes.</para>
        /// </remarks>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            foreach (ITextViewLine line in e.NewOrReformattedLines)
            {
                CreateVisuals(line);
            }
        }

        /// <summary>
        /// Adds a box to the end-of-line whitespace on the given line
        /// </summary>
        /// <param name="line">Line to add the adornments</param>
        private void CreateVisuals(ITextViewLine line)
        {
            // Ignore empty lines
            if (line.Length == 0)  return;

            // Ignore lines that are only whitespace
            var lineText = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(line.Start, line.End)).GetText();
            if (String.IsNullOrWhiteSpace(lineText))  return;
            
            // start with the character at the end of the line
            int charIndex = line.End - 1;
            char charValue = _view.TextSnapshot[charIndex];

            // if it's a tab or space, place a box around it AND all preceding characters of the same value
            // (so consecutive spaces share one box, and consecutive tabs the same)
            while (charIndex >= line.Start && (charValue == ' ' || charValue == '\t'))
            {
                // draw the box and get the index of the "last" character in the box; store the index of the character BEFORE that as the one we'll look at next
                charIndex = AdornConsecutiveCharAndGetLastIndex(charValue, charIndex, line.Start) - 1;

                if (charIndex < 0)  break; // just in case

                // get the character at the next index
                charValue = _view.TextSnapshot[charIndex];

                // with the new values of charIndex and charValue, if the while condition is still true then the next loop will draw another box
            }
        }

        private int AdornConsecutiveCharAndGetLastIndex(char consecutiveChar, int firstIndex, int startOfLine)
        {
            int lastIndex = firstIndex;

            for (int charIndex = firstIndex - 1; charIndex >= startOfLine; --charIndex)
            {
                var charValue = _view.TextSnapshot[charIndex];

                if (charValue == consecutiveChar)
                    lastIndex = charIndex;
                else
                    break;
            }

            // this method loops in reverse, so its "last" and "first" mean the opposite of what they do for AdornSpan
            AdornSpan(lastIndex, firstIndex + 1, GetBrushForChar(consecutiveChar), GetPenForChar(consecutiveChar));

            return lastIndex;
        }
        private void AdornSpan(int spanStart, int spanEnd, Brush brush, Pen pen)
        {
            var span = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(spanStart, spanEnd));
            Geometry geometry = _view.TextViewLines.GetMarkerGeometry(span);
            if (geometry != null)
            {
                var drawing = new GeometryDrawing(brush, pen, geometry);
                drawing.Freeze();

                var drawingImage = new DrawingImage(drawing);
                drawingImage.Freeze();

                var image = new Image
                {
                    Source = drawingImage,
                };

                // Align the image with the top of the bounds of the text geometry
                Canvas.SetLeft(image, geometry.Bounds.Left);
                Canvas.SetTop(image, geometry.Bounds.Top);

                _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
            }
        }

        public void UpdateBrushesAndPens(Brush inSpacesBrush, Pen inSpacesPen, Brush inTabsBrush, Pen inTabsPen)
        {
            _spacesBrush = inSpacesBrush;
            _spacesPen = inSpacesPen;
            _tabsBrush = inTabsBrush;
            _tabsPen = inTabsPen;

            RedrawAllAdornments();
        }

        public void RedrawAllAdornments()
        {
            if (_layer == null || _view == null || _view.TextViewLines == null) { return; }

            //Redraw all adornments
            _layer.RemoveAllAdornments();
            foreach (ITextViewLine line in _view.TextViewLines)
            {
                CreateVisuals(line);
            }
        }
    }
}
