using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Webkit;
using Android.Graphics;
using Android.Content.Res;

namespace presense
{
    [Activity(Label = "presense", MainLauncher = true, Icon = "@drawable/icon")]
    // see  https://developer.android.com/training/sharing/receive.html
    // see https://developer.xamarin.com/api/type/Android.App.IntentFilterAttribute/
    [IntentFilter(new[] { Intent.ActionSend },
        Categories = new[] { Intent.CategoryDefault },
        DataMimeType = "text/plain")]
    public class MainActivity : Activity
    {
        WebView mWebView;

        private IValueCallback mFilePathCallback;
        private static int INPUT_FILE_REQUEST = 1;

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {

            if (requestCode != INPUT_FILE_REQUEST || mFilePathCallback == null)
            {
                base.OnActivityResult(requestCode, resultCode, data);
                return;
            }
            Android.Net.Uri[] results = null;

            if (data != null)
                        {
                            String dataString = data.DataString;
                            if (dataString != null)
                            {
                                results = new Android.Net.Uri[] {Android.Net.Uri.Parse(dataString)};
                            }
                        }
            

            mFilePathCallback.OnReceiveValue(results);
            mFilePathCallback = null;

        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);


            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            mWebView = FindViewById<WebView>(Resource.Id.webView);
            mWebView.Settings.JavaScriptEnabled = true;
            mWebView.Settings.SetGeolocationEnabled(true);

            //mWebView.SetWebViewClient(new MyWebViewClient());
            //Somehow is not necessary
            mWebView.SetWebChromeClient(new MyWebChromeClient(this));




            // the intent stuff
            // Get intent, action and MIME type
            // see

            //Intent intent = getIntent();
            //String action = intent.getAction();
            //String type = intent.getType();

            
            Intent intent = Intent;
            String action = intent.Action;
            String type = intent.Type;

            if (Intent.ActionSend.Equals(action) && type != null)
            {
                if ("text/plain".Equals(type))
                {
                    
                    String toUrl = intent.GetStringExtra(Intent.ExtraText);

                    // für nzz app
                    int index = toUrl.IndexOf('\n');
                    if (index > 0)
                    {
                        toUrl = toUrl.Substring(0, index);
                    }
                    

                    if (toUrl != null)
                    {
                        String url = "https://www.mainig.com/autocreate/acuri?uri=" + toUrl;
                        mWebView.LoadUrl(url);
                    }
                }
            
            } else
            {
                // Load url to be rendered on WebView
                mWebView.LoadUrl("https://www.mainig.com");
            }
        }

        public class MyWebViewClient : WebViewClient
        {

            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                Intent i = new Intent(Intent.ActionView, Android.Net.Uri.Parse(url));
                i.AddFlags(ActivityFlags.NewTask);
                Application.Context.StartActivity(i);
                return true;
            }

        }

        public class MyWebChromeClient : WebChromeClient
        {
            private MainActivity _activity;

            public MyWebChromeClient(MainActivity activity)
            {
                _activity = activity;
            }

            // für den file upload
            public override bool OnShowFileChooser(WebView webView, IValueCallback filePathCallback, FileChooserParams fileChooserParams)
            {
                //return base.OnShowFileChooser(webView, filePathCallback, fileChooserParams);
                if (_activity.mFilePathCallback != null)
                {
                    _activity.mFilePathCallback.OnReceiveValue(null);
                }
                _activity.mFilePathCallback = filePathCallback;

                Intent contentSelectionIntent = new Intent(Intent.ActionGetContent);
                contentSelectionIntent.AddCategory(Intent.CategoryOpenable);
                contentSelectionIntent.SetType("image/*");

                Intent[] intentArray = new Intent[0];

                Intent chooserIntent = new Intent(Intent.ActionChooser);
                chooserIntent.PutExtra(Intent.ExtraIntent, contentSelectionIntent);
                chooserIntent.PutExtra(Intent.ExtraTitle, "Image Chooser");
                chooserIntent.PutExtra(Intent.ExtraInitialIntents, intentArray);



                _activity.StartActivityForResult(chooserIntent, MainActivity.INPUT_FILE_REQUEST);

                return true;
            }

            // Fuer geolocation - https://gist.github.com/Cheesebaron/ad84740c9bffa7e255c8
            public override void OnGeolocationPermissionsShowPrompt(string origin, GeolocationPermissions.ICallback callback)
            {
                const bool remember = false;
                var builder = new AlertDialog.Builder(_activity);
                builder.SetTitle("Location")
                    .SetMessage(string.Format("{0} would like to use your current location", origin))
                    .SetPositiveButton("Allow", (sender, args) => callback.Invoke(origin, true, remember))
                    .SetNegativeButton("Disallow", (sender, args) => callback.Invoke(origin, false, remember));
                var alert = builder.Create();
                alert.Show();
            }




            /*public override void OnProgressChanged(WebView view, int progress)
            {
                //_activity.SetProgress(progress * 100);
                _activity.mProgressBar.Progress = progress;
                if (progress < 100 && _activity.mProgressBar.Visibility == ViewStates.Gone)
                {
                    _activity.mProgressBar.Visibility = ViewStates.Visible;
                }
                else
                {
                    _activity.mProgressBar.Visibility = ViewStates.Gone;
                }
                
            }*/
        }


        // https://developer.xamarin.com/guides/android/user_interface/web_view/
        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back && mWebView.CanGoBack())
            {
                mWebView.GoBack();
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }

        // flipscreen not loading again
        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
        }
    }


}

