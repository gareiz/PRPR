﻿using PRPR.Common;
using PRPR.BooruViewer.Models;
using PRPR.BooruViewer.Services;
using PRPR.BooruViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Documents;
using Windows.Storage.Provider;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.Storage.Pickers;
using System.Net;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PRPR.BooruViewer.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ImagePage : Page
    {
        public ImagePage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;


        }
        #region NavigationHelper

        private NavigationHelper navigationHelper;
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion


        public ImageViewModel ImageViewModel
        {
            get
            {
                return this.DataContext as ImageViewModel;
            }
        }


        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            var b = VisualStateManager.GoToState(CurrentImagePage, "Low", true);
            Debug.WriteLine("NavigationHelper_LoadState");

            // Reset the scroll and zoom of the image
            ImageScrollViewer.ChangeView(null, null, 1.0f, true);//.ZoomToFactor(1);
            //var r = ImageScrollViewer.ChangeView(0, 0, 1, false);

            try
            {
                var x = DisplayInformation.GetForCurrentView();
                if (e.NavigationParameter != null)
                {
                    this.ImageViewModel.Post = Post.FromXml(e.NavigationParameter as string);
                    await ImageViewModel.UpdateIsFavorited();
                    this.ImageViewModel.Comments = await Comments.GetComments(this.ImageViewModel.Post.Id);
                }
            }
            catch (Exception ex)
            {

            }


            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {

                var statusBar = StatusBar.GetForCurrentView();
                if (statusBar != null)
                {
                    await statusBar.HideAsync();
                }
            }
        }


        private async void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {

            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {

                var statusBar = StatusBar.GetForCurrentView();
                if (statusBar != null)
                {
                    await statusBar.ShowAsync();
                }
            }
        }




        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {

            await Windows.System.Launcher.LaunchUriAsync(new Uri($"https://yande.re/post/show/{ImageViewModel.Post.Id}"));
        }

        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var scrollViewer = (sender as ScrollViewer);
            (scrollViewer.Content as FrameworkElement).MaxHeight = e.NewSize.Height;
            (scrollViewer.Content as FrameworkElement).MaxWidth = e.NewSize.Width;
        }


















        private async Task SaveImageFileAsync(string fileUri, string fileExtension)
        {

            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            string type = $".{fileExtension}";
            savePicker.FileTypeChoices.Add(type, new List<string>() { type });
            savePicker.SuggestedFileName = GetFileName(fileUri);
            
            StorageFile file = await savePicker.PickSaveFileAsync();
            var imageBuffer = await (new Windows.Web.Http.HttpClient()).GetBufferAsync(new Uri(fileUri));


            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
                await FileIO.WriteBufferAsync(file, imageBuffer);
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);

                if (status != FileUpdateStatus.Complete)
                {
                    await new MessageDialog("File " + file.Name + " couldn't be saved.").ShowAsync();
                }
            }
        }

        private static string GetFileName(string uri)
        {
            var output = uri;
            if (output.LastIndexOf('/') >= 0)
            {
                output = output.Substring(output.LastIndexOf('/'));
            }
            if (output.LastIndexOf('0') >= 0)
            {
                output = output.Substring(0, output.LastIndexOf('.'));
            }
            return WebUtility.UrlDecode(output);
        }

        private async void DownloadSampleButton_Click(object sender, RoutedEventArgs e)
        {
            //await Windows.System.Launcher.LaunchUriAsync(new Uri($"{ImageViewModel.Post.SampleUrl}"));
            await SaveImageFileAsync(ImageViewModel.Post.SampleUrl, "jpg");
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            //await Windows.System.Launcher.LaunchUriAsync(new Uri($"{ImageViewModel.Post.FileUrl}"));
            await SaveImageFileAsync(ImageViewModel.Post.FileUrl, ImageViewModel.Post.FileExtension);
        }

        private async void DownloadJpegButton_Click(object sender, RoutedEventArgs e)
        {
            //await Windows.System.Launcher.LaunchUriAsync(new Uri($"{ImageViewModel.Post.JpegUrl}"));
            await SaveImageFileAsync(ImageViewModel.Post.JpegUrl, "jpg");

        }









        
        private async void SetWallPaperButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await PersonalizationHelper.SetWallPaper(this.ImageViewModel.Post);

            }
            catch (Exception ex)
            {
                
            }
        }

        

        private void SetLockScreenButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LockScreenPreviewPage), this.ImageViewModel.Post.ToXml());
              
        }

        private async void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {

            try
            {

                await ImageViewModel.Favorite();
            }
            catch (Exception ex)
            {
                
            }


        }













        private async void UnfavoriteButton_Click(object sender, RoutedEventArgs e)
        {


            await ImageViewModel.Unfavorite();
        }















        private void SampleImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            var b = VisualStateManager.GoToState(CurrentImagePage, "Medium", true);
            Debug.WriteLine("SampleImage_ImageOpened");
        }

        private void JpegImage_ImageOpened(object sender, RoutedEventArgs e)
        {

            var b =VisualStateManager.GoToState(CurrentImagePage, "High", true);
            Debug.WriteLine("JpegImage_ImageOpened");
        }






















        bool ButtonsOverlaying = true;
        bool DetailsOverlaying = false;
        private void MainPreview_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Debug.WriteLine("MainPreview_Tapped");


            // Toggle the buttons display
            if (!ButtonsOverlaying)
            {
                ButtonsOverlaying = true;
                var b = VisualStateManager.GoToState(CurrentImagePage, "ButtonsOnly", true);
            }
            else
            {
                if (DetailsOverlaying)
                {
                    DetailsOverlaying = false;
                    var b = VisualStateManager.GoToState(CurrentImagePage, "ButtonsOnly", true);
                }
                else
                {
                    ButtonsOverlaying = false;
                    var b = VisualStateManager.GoToState(CurrentImagePage, "NoOverlay", true);
                }
            }


            e.Handled = true;
        }
        

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (DetailsOverlaying)
            {
                DetailsOverlaying = false;
                var b = VisualStateManager.GoToState(CurrentImagePage, "ButtonsOnly", true);
            }
            else
            {
                DetailsOverlaying = true;
                var b = VisualStateManager.GoToState(CurrentImagePage, "Details", true);
            }
        }
        
        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            await Windows.System.Launcher.LaunchUriAsync(new Uri($"https://yande.re/post/show/{ImageViewModel.Post.Id}"));
        }
        

        private void AuthorButton_Click(object sender, RoutedEventArgs e)
        {
            // Search tags
            this.Frame.Navigate(typeof(HomePage), $"user:{this.ImageViewModel.Post.Author}");
        }

        private void TagsWrapBlock_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshTagsWrapBlock(sender as RichTextBlock);
        }

        private void TagsWrapBlock_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            RefreshTagsWrapBlock(sender as RichTextBlock);
        }

        void RefreshTagsWrapBlock(RichTextBlock tagsWrapBlock)
        {
            tagsWrapBlock.Blocks.Clear();
            var groupedTags = this.ImageViewModel.Post.TagItems.GroupBy(o => o.Type);
            foreach (var group in groupedTags)
            {
                var p = new Paragraph();

                var gOrdered = group.OrderBy(o => o.Name.Length);
                //var zipped = gOrdered.Take(gOrdered.Count() / 2).Zip(gOrdered.Skip(gOrdered.Count() / 2).Reverse(), (f, s) => new List<TagDetail>() { f, s }).SelectMany(i => i);
                foreach (var item in gOrdered)
                {
                    var button = new ContentControl() { ContentTemplate = this.Resources["TagButtonTemplate"] as DataTemplate, DataContext = item };
                    var i = new InlineUIContainer() { Child = button };
                    p.Inlines.Add(i);
                }
                tagsWrapBlock.Blocks.Add(p);
            }
        }
        
        private void TagButton_Click(object sender, RoutedEventArgs e)
        {

            // Search tags
            this.Frame.Navigate(typeof(HomePage), $"{((sender as Button).DataContext as TagDetail).Name}");
        }
        

        private void WidthStates_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            if (e.NewState == Width480Height500 || e.NewState == Width700)
            {
                BackgroundBackDrop.BlurAmount = 5;
            }
            else
            {
                BackgroundBackDrop.BlurAmount = 2;
            }
        }

        private void ImageScrollViewer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Debug.WriteLine("ImageScrollViewer_DoubleTapped");

            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer.ZoomFactor != 1)
            {
                scrollViewer.ChangeView(null, null, 1, true);
            }
            else
            {
                var point2 = e.GetPosition(scrollViewer);


                var point = e.GetPosition(scrollViewer.Content as FrameworkElement);


                var bw = scrollViewer.ActualWidth;
                var bh = scrollViewer.ActualHeight;
                var iw = (scrollViewer.Content as FrameworkElement).ActualWidth;
                var ih = (scrollViewer.Content as FrameworkElement).ActualHeight;



                double? y = point.Y;
                double? x = point.X;
                if (bw / bh > iw / ih)
                {
                    if (iw * 2 < bw)
                    {
                        x = null;
                    }
                    else
                    {
                        x = point.X * 2 - (bw / 2 - (iw / 2 - point.X));

                    }
                }
                else
                {
                    if (ih * 2 < bh)
                    {
                        y = null;
                    }
                    else
                    {
                        y = point.Y * 2 - (bh / 2 - (ih / 2 - point.Y));
                    }
                }
                
                scrollViewer.ChangeView(x, y, 2, true);
            }

            e.Handled = true;
        }

        private void BackgroundBackDrop_Loaded(object sender, RoutedEventArgs e)
        {

            if (WidthStates.CurrentState == Width480Height500 || WidthStates.CurrentState == Width700)
            {
                BackgroundBackDrop.BlurAmount = 5;
            }
            else
            {
                BackgroundBackDrop.BlurAmount = 2;
            }

        }
        
    }
}
