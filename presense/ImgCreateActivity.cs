using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;
using Android.Graphics;
using Android.Provider;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace presense
{
    [Activity(Label = "Image Upload", Theme = "@style/presenseTheme", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class ImgCreateActivity : Activity
    {

        const string TAG = "ImgCreateActivity";
        private int GET_CONTENT_REQUEST = 2;
        private string cookie;
        private Bitmap bitmap = null;
        private CookieContainer cookieContainer;
        private ProgressBar progressBar;

        //private PsProcess[] processArray;

        private String processId = null;
        



        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // getCookie
            // Retrieve the parameter.
            cookie = Intent.GetStringExtra("cookie");

            Log.Debug(TAG, "cookie: " + cookie);
            cookieContainer = new CookieContainer();
            cookieContainer.SetCookies(new Uri(MainActivity.DOMAIN), cookie);


            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.ImgCreate);

            // Create your application here

            progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar1);



            Button button2 = FindViewById<Button>(Resource.Id.button2);



            button2.Click += delegate
            {
                if ( bitmap == null )
                {
                    return;
                }

                // no more actions, upload in progress
                button2.Visibility = ViewStates.Gone;
                progressBar.Visibility = ViewStates.Visible;





                // b64
                MemoryStream stream = new MemoryStream();
                bitmap.Compress(Bitmap.CompressFormat.Jpeg, 70, stream);
                byte[] ba = stream.ToArray();
                string b64String = Base64.EncodeToString(ba, Base64Flags.Default);


                // create JSON
                JObject oDataEntry = new JObject
                {
                    { "b64string", b64String },
                    { "type", "image/jpeg" }
                };

                JArray dataArray = new JArray
                {
                    oDataEntry
                };

                JObject oInputEntry = new JObject
                {
                    { "key", "img" },
                    { "data", dataArray }
                };

                JArray oInputArray = new JArray
                {
                    oInputEntry
                };


                JObject oJsonObject = new JObject
                {
                    { "form", "createXidForm" },
                    { "inputArray", oInputArray }
                };

                // request
                String sUrl = MainActivity.DOMAIN + "/rest/gxself";
                String sContentType = "application/json";

                HttpClient oHttpClient = new HttpClient(new HttpClientHandler() { CookieContainer = cookieContainer });


                var oTaskPostAsync = oHttpClient.PostAsync(sUrl, new StringContent(oJsonObject.ToString(), Encoding.UTF8, sContentType));
                oTaskPostAsync.ContinueWith((oHttpPostTask) =>
                {
                    HttpResponseMessage httpResponseMessage = oHttpPostTask.Result;
                    // response of post here

                    Log.Debug(TAG, "Request: " + httpResponseMessage.RequestMessage);
                    Log.Debug(TAG, "StatusCode: " + httpResponseMessage.StatusCode);
                    Log.Debug(TAG, "Header: " + httpResponseMessage.Headers);

                    var contentAsync = httpResponseMessage.Content.ReadAsStringAsync();
                    contentAsync.ContinueWith((oReadContentTask) =>
                    {
                        String result = oReadContentTask.Result;
                        JsonResult jsonResult = JsonConvert.DeserializeObject<JsonResult>(result);

                        Log.Debug(TAG, "Content: " + jsonResult.newEntityId);





                        // finish
                        Intent goBackIntent = new Intent(this, typeof(MainActivity));


                        // finish process and start new


                        // this.processArray[0] = new PsProcess("imagedescription", "/x-create", null, jsonResult.newEntityId);
                        // goBackIntent.PutExtra("processArray", JsonConvert.SerializeObject(this.processArray));


                        goBackIntent.PutExtra("processId", processId);
                        goBackIntent.PutExtra("newEntityId", jsonResult.newEntityId);
                    

                        SetResult(Result.Ok, goBackIntent);
                        Finish();

                    });

                });

                
            };

            String imageUri = Intent.GetStringExtra("imageUri");


            processId = Intent.GetStringExtra("processId");



            // processArray = JsonConvert.DeserializeObject<PsProcess[]>(Intent.GetStringExtra("processArray"));



            if (imageUri != null)
            {
                Android.Net.Uri passedUri = Android.Net.Uri.Parse(imageUri);
                ReadAndScaleBitmap(passedUri);
            } else
            {
                LoadBitmap();
            }

            
        }

        private void LoadBitmap()
        {

            Intent createImgIntent = new Intent();
            createImgIntent.SetType("image/*");
            // Show only images, no videos or anything else
            createImgIntent.SetAction(Intent.ActionGetContent);
            // Always show the chooser (if there are multiple options available)

            StartActivityForResult(createImgIntent, GET_CONTENT_REQUEST);
        }

        public class JsonResult
        {
            public Object BEdocuments { get; set; }
            public String newEntityId { get; set; }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == GET_CONTENT_REQUEST && resultCode == Result.Ok && data != null && data.Data != null)
            {
                Android.Net.Uri uri = data.Data;

                ReadAndScaleBitmap(uri);



            }
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back)
            {
                SetResult(Result.Canceled);
                Finish();
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }

        private void ReadAndScaleBitmap(Android.Net.Uri uri)
        {
            try
            {
                bitmap = MediaStore.Images.Media.GetBitmap(this.ContentResolver, uri);


                const int maxSize = 2560;
                int newWidth;
                int newHeight;
                if (bitmap.Width > bitmap.Height)
                {
                    newWidth = maxSize;
                    newHeight = (bitmap.Height * maxSize) / bitmap.Width;
                }
                else
                {
                    newHeight = maxSize;
                    newWidth = (bitmap.Width * maxSize) / bitmap.Height;
                }

                if (newHeight < bitmap.Height || newWidth < bitmap.Width)
                {
                    bitmap = Bitmap.CreateScaledBitmap(bitmap, newWidth, newHeight, false);
                }



                Button button2 = FindViewById<Button>(Resource.Id.button2);
                button2.Visibility = ViewStates.Visible;

                ImageView imageView = FindViewById<ImageView>(Resource.Id.imageView1);
                imageView.SetImageBitmap(bitmap);



            }
            catch (InvalidOperationException e)
            {
                Log.Error(TAG, e.ToString());
            }
        }

    }


}