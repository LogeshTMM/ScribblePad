using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ScribblePad {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      Point mPoint = new ();
      bool mIsDrawing, mSingleScribble = true, mMagicLine,
         mLeftButtonPressed;
      readonly Brush mStroke = Brushes.White;
      readonly Polyline polyLine = new ();
      readonly PointCollection pointCollection = new ();

      public MainWindow () {
         InitializeComponent ();
      }

      private void Canvas_MouseLeftButtonDown (object sender, MouseButtonEventArgs e) {
         if (mSingleScribble && sender is Canvas) ScribblePad.Children.Clear ();
         if ((SingleScribble.IsChecked == true || MultiScribble.IsChecked == true) && sender is Canvas) {
            mIsDrawing = true;
            mPoint = e.GetPosition (ScribblePad);
         } else if (sender is Canvas && mMagicLine == true) {
            mLeftButtonPressed = true;
            mIsDrawing = false;
            pointCollection.Clear ();
         }
      }

      private void MenuItem_Exit_Click (object sender, RoutedEventArgs e) {
         if (sender is MenuItem) Close ();
      }

      private void ScribblePad_MouseMove (object sender, MouseEventArgs e) {
         if (mIsDrawing && sender is Canvas) {
            Line line = new () { X1 = mPoint.X, Y1 = mPoint.Y };
            mPoint = e.GetPosition (ScribblePad);
            line.X2 = mPoint.X;
            line.Y2 = mPoint.Y;
            line.Stroke = mStroke;
            line.StrokeThickness = 4;
            ScribblePad.Children.Add (line);

         } else if (mLeftButtonPressed == true && mMagicLine && sender is Canvas) {
            pointCollection.Add (e.GetPosition (ScribblePad));
            polyLine.Points = pointCollection;
            polyLine.Stroke = mStroke;
            polyLine.StrokeThickness = 4;
         }
      }

      private void RadioButton_Click (object sender, RoutedEventArgs e) {
         if (SingleScribble.IsChecked == true) {
            mSingleScribble = true;
            mMagicLine = false;
         }
      }
      private void MultiScribble_Click (object sender, RoutedEventArgs e) {
         if (MultiScribble.IsChecked == true) {
            mSingleScribble = false;
            mMagicLine = false;
         }
      }

      private void MagicLine_Click (object sender, RoutedEventArgs e) {
         if (MagicLine.IsChecked == true) {
            mMagicLine = true;
            mIsDrawing = false;
         }
      }

      private void ScribblePad_MouseLeftButtonUp (object sender, MouseButtonEventArgs e) {
         mIsDrawing = false;
         mLeftButtonPressed = false;
         if (sender is Canvas && mMagicLine == true)
            ScribblePad.Children.Add (polyLine);
      }
   }
}
