﻿using HtmlAgilityPack;
using PRPR.Common.Models;
using PRPR.ExReader.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace PRPR.ExReader.Models
{
    public class ExGallery : ObservableCollection<ExGalleryImageListItem>, ISupportIncrementalLoading, IImageWallItemImage
    {
        public string Gid
        {
            get
            {
                return this.Link.Replace("https://exhentai.org/g/", "").Split('/')[0];
            }
        }

        public string Token
        {
            get
            {
                return this.Link.Replace("https://exhentai.org/g/", "").Split('/')[1];
            }
        }

        public string Title { get; set; }
        public string JapaneseTitle { get; set; }
        public string Category { get; set; }
        public string Link { get; set; }


        public string Thumb { get; set; }
        public string Uploader { get; set; }
        public double Rating { get; set; }
        public int TorrentCount { get; set; }

        // Details
        public int FileCount { get; set; }

        public string Language { get; set; }

        public bool HasMoreItems
        {
            get
            {
                return this.CurrentPageNumber < PageCount;
            }
        }

        public int PageCount { get; set; } = 0;

        private int CurrentPageNumber { get; set; } = 0;



        // TODO: idk 
        public bool IsFavorited { get; set; } = false;



        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            return AsyncInfo.Run((c) => LoadMoreItemsAsync(c, count));
        }



        public ObservableCollection<ExTag> Tags { get; set; } = new ObservableCollection<ExTag>();


        public ObservableCollection<ExComment> Comments { get; } = new ObservableCollection<ExComment>();


        public async Task LoadAllItemsAsync()
        {

            for (int i = CurrentPageNumber + 1; i <= Count; i++)
            {

            }

            while (this.PageCount != this.CurrentPageNumber)
            {
                await LoadMoreItemsAsync(new CancellationToken(), 0);
            }
        }





        async Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count)
        {
            try
            {
                var nextPageList = await DownloadGalleryAsync(this.Link, this.CurrentPageNumber + 1, 3);

                if (nextPageList.CurrentPageNumber == this.CurrentPageNumber + 1)
                {
                    foreach (var item in nextPageList)
                    {
                        this.Add(item);
                    }

                    this.CurrentPageNumber = nextPageList.CurrentPageNumber;
                    this.PageCount = nextPageList.PageCount;
                    return new LoadMoreItemsResult { Count = (uint)nextPageList.Count };
                }
                else
                {
                    return new LoadMoreItemsResult { Count = 0 };
                }

            }
            catch (Exception ex)
            {
                return new LoadMoreItemsResult { Count = 0 };
            }
        }




        public DateTime Published { get; set; }

        public int PreferredWidth
        {
            get
            {
                if (String.IsNullOrEmpty(Thumb))
                {
                    return 0;
                }
                else
                {
                    //https://exhentai.org/t/84/a4/84a4f79496beaee4fde47184604e864ccddbf9f0-639417-2560-3629-jpg_l.jpg
                    string numbers = Thumb.Split('-')[2];
                    if (int.Parse(numbers) == 0)
                    {

                    }
                    return int.Parse(numbers) / 10;
                }
            }
        }

        public int PreferredHeight
        {
            get
            {
                if (String.IsNullOrEmpty(Thumb))
                {
                    return 0;
                }
                else
                {
                    string numbers = Thumb.Split('-')[3];
                    if (int.Parse(numbers) == 0)
                    {

                    }
                    return int.Parse(numbers) / 10;
                }
            }
        }

        public static async Task<ExGallery> DownloadGalleryAsync(string link, int pagenumber, uint maxAttempt = 1)
        {
            uint attempt = 1;
            string htmlString = null;
            while (attempt <= maxAttempt && htmlString == null)
            {
                try
                {
                    // Get page html
                    htmlString = await ExClient.GetStringWithExCookie($"{link}?p={pagenumber - 1}", "ts_l");
                }
                catch (HttpRequestException ex)
                {
                    if (attempt == maxAttempt)
                    {
                        // Last attempt
                        throw;
                    }
                    else
                    {
                        attempt++;
                    }
                }
                catch (Exception ex)
                {

                }
            }
            
            return GetExGalleryFromHtml(link, htmlString);
        }


        public static ExGallery GetExGalleryFromHtml(string link, string htmlSource)
        {
            if (htmlSource == null)
            {
                return null;
            }

            var l = new ExGallery() { Link = link };
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.OptionFixNestedTags = true;
            htmlDocument.LoadHtml(htmlSource);
            try
            {
                HtmlNodeCollection imageNodes = htmlDocument.DocumentNode.SelectNodes("//div[@id='gdt']/div[@class!='c']");
                foreach (var node in imageNodes)
                {
                    l.Add(ExGallery.GetImageListItemFromNode(node));
                }
                l.Rating = ReadRating(htmlDocument);
                l.Title = ReadTitle(htmlDocument);
                l.JapaneseTitle = ReadJapaneseTitle(htmlDocument);
                l.PageCount = ReadPageCount(htmlDocument);
                l.CurrentPageNumber = ReadCurrentPageNumber(htmlDocument);
                l.FileCount = ReadFileCount(htmlDocument);
                l.Tags = ReadTags(htmlDocument);
                l.Language = ReadLanguage(htmlDocument);
                l.IsFavorited = ReadIsFavorited(htmlDocument);
                l.Thumb = ReadThumb(htmlDocument);

                HtmlNodeCollection commentNodes = htmlDocument.DocumentNode.SelectNodes("//div[@class='c1']");
                if (commentNodes != null)
                {
                    foreach (var node in commentNodes)
                    {
                        l.Comments.Add(ExComment.FromNode(node));
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return l;
        }

        private static string ReadThumb(HtmlDocument htmlDocument)
        {
            var c = htmlDocument.DocumentNode.SelectSingleNode(".//div[@id='gd1']//img");
            
            if (c != null)
            {
                return c.GetAttributeValue("src", null); ;
            }
            else
            {
                throw new Exception("Cannot parse thumb");
            }
            
        }

        private static int ReadFileCount(HtmlDocument htmlDocument)
        {
            var c = htmlDocument.DocumentNode.SelectSingleNode("//div[@id='gdd']//tr[./td/text()='Length:']/td[last()]");
            if (c != null)
            {
                return int.Parse(c.InnerText.Replace(" pages", ""));
            }
            else
            {
                return 0;
            }

            
        }

        private static ExGalleryImageListItem GetImageListItemFromNode(HtmlNode node)
        {
            var c = node.SelectSingleNode(".//img");

            if (c != null)
            {
                return new ExGalleryImageListItem()
                {
                    Link = c.ParentNode.GetAttributeValue("href", ""),
                    Thumb = c.GetAttributeValue("src", "")   
                };
            }
            else
            {
                throw new Exception("Cannot parse image list item");
            }
        }

        private static double ReadRating(HtmlDocument htmlDocument)
        {
            var c = htmlDocument.DocumentNode.SelectSingleNode("//td[@id='rating_label']");

            if (c != null)
            {
                return double.Parse(WebUtility.HtmlDecode(c.InnerText).Replace("Average: ", ""));
            }
            else
            {
                throw new Exception("Cannot parse title");
            }
        }

        private static string ReadTitle(HtmlDocument htmlDocument)
        {
            var c = htmlDocument.DocumentNode.SelectSingleNode("//h1[@id='gn']");

            if (c != null)
            {
                return WebUtility.HtmlDecode(c.InnerText);
            }
            else
            {
                throw new Exception("Cannot parse title");
            }
        }

        private static string ReadJapaneseTitle(HtmlDocument htmlDocument)
        {
            var c = htmlDocument.DocumentNode.SelectSingleNode("//h1[@id='gj']");

            if (c != null)
            {
                return WebUtility.HtmlDecode(c.InnerText);
            }
            else
            {
                throw new Exception("Cannot parse japanese title");
            }
        }

        private static int ReadPageCount(HtmlDocument htmlDocument)
        {
            var c = htmlDocument.DocumentNode.SelectSingleNode("//table[@class='ptt']//td[last()-1]/a");
            if (c != null)
            {
                return int.Parse(c.InnerText);
            }
            else
            {
                return 0;
            }
        }

        private static string ReadLanguage(HtmlDocument htmlDocument)
        {
            var c = htmlDocument.DocumentNode.SelectSingleNode("//div[@id='gdd']//tr[./td/text()='Language:']/td[last()]/text()");
            if (c != null)
            {
                return WebUtility.HtmlDecode(c.InnerText).Trim();
            }
            else
            {
                return null;
            }
        }


        private static bool ReadIsFavorited(HtmlDocument htmlDocument)
        {
            var c = htmlDocument.DocumentNode.SelectSingleNode("//a[@id='favoritelink']/text()");
            if (c != null)
            {
                return !WebUtility.HtmlDecode(c.InnerText).Contains("Add");
            }
            else
            {
                return false;
            }
        }


        private static ObservableCollection<ExTag> ReadTags(HtmlDocument htmlDocument)
        {
            ObservableCollection<ExTag> tags = new ObservableCollection<ExTag>();

            var nodes = htmlDocument.DocumentNode.SelectNodes("//a[starts-with(@id, 'ta_')]");
            foreach (var node in nodes)
            {
                var fullName = WebUtility.UrlDecode(node.GetAttributeValue("href", "").Replace("https://exhentai.org/tag/", ""));
                tags.Add(new ExTag(fullName));
            }

            return tags;
        }




        private static int ReadCurrentPageNumber(HtmlDocument htmlDocument)
        {
            var c = htmlDocument.DocumentNode.SelectSingleNode("//table[@class='ptt']//td[@class='ptds']/a");
            if (c != null)
            {
                return int.Parse(c.InnerText);
            }
            else
            {
                return 0;
            }
        }


        public static ExGallery GetGalleryListItemFromNode(HtmlNode node)
        {

            var g = new ExGallery();

            g.Title = WebUtility.HtmlDecode(node.SelectSingleNode(".//div[@class='id2']/a").InnerText);
            g.Link = node.SelectSingleNode(".//div[@class='id2']/a").GetAttributeValue("href", null);
            g.FileCount = int.Parse(node.SelectSingleNode(".//div[@class='id42']").InnerText.Replace(" files", ""));
            g.Category = node.SelectSingleNode(".//div[@class='id41']").GetAttributeValue("title", "");
            g.Thumb = node.SelectSingleNode(".//div[@class='id3']//img").GetAttributeValue("src", null);
            g.Rating = GetRatingFromStars(node);
            return g;
        }

        private static double GetRatingFromStars(HtmlNode node)
        {
            // background-position:-16px -21px; opacity:1; margin-top:2px
            var style = node.SelectSingleNode(".//div[starts-with(@class,'id43')]").GetAttributeValue("style", null);
            Debug.WriteLine(style);
            var splited = style.Split(';');
            var numbers = splited[0].Replace("background-position:", "").Replace("px", ",").Split(',');

            return 5.0 * (80.0 + int.Parse(numbers[0])) / 80.0 - 0.5 * ((int.Parse(numbers[1]) + 1)) / -20.0;
        }
    }

    

    public class ExGalleryImageListItem : IImageWallItemImage
    {
        public string Link { get; set; }

        public string Thumb { get; set; }

        

        public int PreferredWidth
        {
            get
            {
                if (String.IsNullOrEmpty(Thumb))
                {
                    return 0;
                }
                else
                {
                    //http://37.48.116.168/1c/fc/1cfcf82cc3ff1c7903cbaf474e6647c906ee46e6-396324-800-800-jpg_l.jpg
                    var splited = Thumb.Split('-');
                    int w = 0;
                    if (splited.Length >= 4)
                    {
                        int.TryParse(splited[splited.Length - 3], out w);
                    }
                    return w;
                }
            }
        }

        public int PreferredHeight
        {
            get
            {
                if (String.IsNullOrEmpty(Thumb))
                {
                    return 0;
                }
                else
                {
                    //http://37.48.116.168/1c/fc/1cfcf82cc3ff1c7903cbaf474e6647c906ee46e6-396324-800-800-jpg_l.jpg
                    var splited = Thumb.Split('-');
                    int h = 0;
                    if (splited.Length >= 4)
                    {
                        int.TryParse(splited[splited.Length - 2], out h);
                    }
                    return h;
                }
            }
        }
    }
}
