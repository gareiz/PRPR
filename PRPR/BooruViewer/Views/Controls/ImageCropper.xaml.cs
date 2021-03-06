﻿using Imaging;
using PRPR.BooruViewer.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace PRPR.BooruViewer.Views.Controls
{
    public sealed partial class ImageCropper : UserControl
    {
        public ImageCropper()
        {
            this.InitializeComponent();
            
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (InnerImage.ActualWidth != 0)
            {
                // Apply crop
                UpdatePosition(true);
            }
            
        }

        private void InnerImage_ImageOpened(object sender, RoutedEventArgs e)
        {           
            
            if (InnerImage.ActualWidth != 0)
            {
                // Apply crop
                UpdatePosition(false);
            }
        }

        private List<Rect> _rects = null;
        public List<Rect> Rects
        {
            get
            {
                return _rects;
            }
            set
            {
                _rects = value;

                // Apply crop
                UpdatePosition(false);
            }
        }


        void UpdatePosition(bool isAnimated)
        {
            var imageSize = new Size(InnerImage.ActualWidth, InnerImage.ActualHeight);
            var targetSize = new Size(this.ActualWidth, this.ActualHeight);
            var proxySize = new Size(ProxyImage.ActualWidth, ProxyImage.ActualHeight);
            var cropRect = CropBitmap.GetGreedyCropRect(imageSize, targetSize, ScaleFaces(Rects, imageSize, proxySize));


            if (cropRect.Width == 0 || cropRect.Height == 0 || proxySize.Width == 0 || targetSize.Width == 0)
            {
                return;
            }

            try
            {
                var w = Resizer.HorizontalOffset;
                var h = Resizer.VerticalOffset;
                

                var s = Resizer.ChangeView(cropRect.Left * targetSize.Width / cropRect.Width,
                    cropRect.Top * targetSize.Width / cropRect.Width,
                    Convert.ToSingle(targetSize.Width / cropRect.Width),
                    true//!isAnimated
                    );
            }
            catch (Exception ex)
            {
                
            }
            
        }

        
       
        public string ProxySource
        {
            get
            {
                return (string)GetValue(ProxySourceProperty);
            }
            set
            {
                SetValue(ProxySourceProperty, value);
            }
        }

        public string ImageSource
        {
            get
            {
                return (string)GetValue(ImageSourceProperty);
            }
            set
            {
                SetValue(ImageSourceProperty, value);
            }
        }

        //private static void OnProxyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    try
        //    {

        //        ImageCropper imageCropper = d as ImageCropper;
        //        String s = e.NewValue as String;
        //        if (s != String.Empty)
        //        {
        //            imageCropper.ProxySource = s;
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}
        //private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    try
        //    {

        //        ImageCropper imageCropper = d as ImageCropper; //null checks omitted
        //        String s = e.NewValue as String; //null checks omitted
        //        if (s != String.Empty)
        //        {
        //            imageCropper.ImageSource = s;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
                
        //    }
        //}

        public static readonly DependencyProperty ProxySourceProperty =
            DependencyProperty.Register(nameof(ProxySource), typeof(string), typeof(ImageCropper), null);// new PropertyMetadata(null, new PropertyChangedCallback(OnProxyChanged)));


        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(nameof(ImageSource), typeof(string), typeof(ImageCropper), null);

        private void InnerImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (InnerImage.ActualWidth != 0)
            {
                // Cal crop
                UpdatePosition(true);
            }
        }


        public static IEnumerable<Rect> ScaleFaces(IEnumerable<Rect> faces, Size imageSize, Size proxySize)
        {
            if (faces == null || faces.Count() == 0)
            {
                return faces;
            }
            else
            {
                List<Rect> rects = new List<Rect>();
                var scaleX = imageSize.Width / proxySize.Width;
                var scaleY = imageSize.Height / proxySize.Height;
                foreach (var rect in faces)
                {
                    rects.Add(new Rect(scaleX * rect.Left, scaleY * rect.Top, scaleX * rect.Width, scaleY * rect.Height));
                }
                return rects;
            }
        }

        private async void ProxyImage_ImageOpened(object sender, RoutedEventArgs e)
        {            
            // Download
            IBuffer proxyResult = null;

            HttpClient client = new HttpClient();
            try
            {
                proxyResult = await client.GetBufferAsync(new Uri(ProxySource));
            }
            catch (Exception ex)
            {
                return;
            }

            // Detect
            var factor = 1.05;
            var min = 3;
            var size = 25;
            AnimeFaceDetector c;
            c = new AnimeFaceDetector();
            BitmapDecoder bd = await BitmapDecoder.CreateAsync(proxyResult.AsStream().AsRandomAccessStream());
            BitmapFrame bf = await bd.GetFrameAsync(0);
            
            
            try
            {
                var s = await c.DetectBitmap(bf, factor, min, new Size(size, size));
                Rects = s.ToList();
            }
            catch (Exception ex)
            {

            }

            
        }




        bool ImageLoaded = false;
        private void InnerImage_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ProxyImage_Loaded");

            ImageLoaded = true;
            
            // Smooth zoom
            UpdatePosition(false);
        }
        
        private void InnerImage_Unloaded(object sender, RoutedEventArgs e)
        {
            ImageLoaded = false;
        }




        bool ProxyLoaded = false;

        private void ProxyImage_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ProxyImage_Loaded");

            ProxyLoaded = true;
            
            UpdatePosition(false);
        }

        private void ProxyImage_Unloaded(object sender, RoutedEventArgs e)
        {
            ProxyLoaded = false;
        }






        bool ResizerLoaded = false;

        private void Resizer_Loaded(object sender, RoutedEventArgs e)
        {
            ResizerLoaded = true;
            
            UpdatePosition(false);
        }

        private void Resizer_Unloaded(object sender, RoutedEventArgs e)
        {
            ResizerLoaded = false;
        }

        private void ProxyImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePosition(false);
        }
    }
}
