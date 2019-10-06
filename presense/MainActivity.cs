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
using Firebase.Iid;
using System.Collections.Generic;
using Android.Util;
using Java.Util.Regex;
using Java.Interop;
using System.Threading;
using Android.Net;
using Android.Support.V4.App;
using Android.Content.PM;
using Newtonsoft.Json;

namespace presense
{
    [Activity(MainLauncher = true, Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    // see  https://developer.android.com/training/sharing/receive.html
    // see https://developer.xamarin.com/api/type/Android.App.IntentFilterAttribute/
    [IntentFilter(new[] { Intent.ActionSend },
        Categories = new[] { Intent.CategoryDefault },
        DataMimeType = "text/plain"),
     IntentFilter(new[] { Intent.ActionSend },
        Categories = new[] { Intent.CategoryDefault },
        DataMimeType = "image/*"),
     IntentFilter(new[] { Intent.ActionView },
        AutoVerify = false, // do this at some point later https://xamarinhelp.com/android-app-links/
        Categories = new[] {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
         },
        DataScheme = "http",
        DataHost = "presense.io"
     ),
     IntentFilter(new[] { Intent.ActionView },
        Categories = new[] {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
         },
        DataScheme = "https",
        DataHost = "presense.io"
     ),
     IntentFilter(new[] { Intent.ActionView },
        Categories = new[] {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
         },
        DataScheme = "http",
        DataHost = "www.presense.io"
     ),
     IntentFilter(new[] { Intent.ActionView },
        Categories = new[] {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
         },
        DataScheme = "https",
        DataHost = "www.presense.io"
     )]
    public class MainActivity : Activity
    {
        public WebView mWebView;

//        private IValueCallback mFilePathCallback;
//        private static int INPUT_FILE_REQUEST = 1;
//        public static int IMG_CREATE_REQUEST_FOR_ATIVITY = 2;
        public static int IMG_CREATE_REQUEST = 3;

        static readonly int PERMISSION_REQUEST_FINE_LOCATION = 0;

        public static String DOMAIN = "https://www.presense.io";

        const string TAG = "MainActivity";

        public PsProcess[] processArray;


 //       private Handler mHandler;

        MyBroadcastReceiver mBroadcasReceiver;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // get Broadcast Receiver
            mBroadcasReceiver = new MyBroadcastReceiver(this);


            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            mWebView = FindViewById<WebView>(Resource.Id.webView);
            mWebView.Settings.JavaScriptEnabled = true;
            mWebView.Settings.SetGeolocationEnabled(true);

            //mWebView.SetWebViewClient(new MyWebViewClient());
            //Somehow is not necessary
            mWebView.SetWebChromeClient(new MyWebChromeClient(this));


            mWebView.AddJavascriptInterface(new WebAppInterface(this), "Android");



            // try set cookies
            try
            {
                var firebaseToken = FirebaseInstanceId.Instance.Token;
                if (firebaseToken != null)
                {
                    String cookieFirebaseToken = "firebaseToken=" + firebaseToken;
                    CookieManager.Instance.SetCookie(MainActivity.DOMAIN, cookieFirebaseToken);
                }
            } catch (Exception e)
            {
                Log.Error(TAG, e.ToString());
            }







            // the intent stuff
            // Get intent, action and MIME type
            // seeAus

            //Intent intent = getIntent();
            //String action = intent.getAction();
            //String type = intent.getType();


            Intent intent = Intent;
            String action = intent.Action;
            String type = intent.Type;


            if (Intent.ActionSend.Equals(action) && type != null && "text/plain".Equals(type))
            {
                String extraText = intent.GetStringExtra(Intent.ExtraText);

                // für nzz app und vieles mehr (eigentlich sollte es eine weiche geben -> ev auch einfach beitrag mit diesem text)
                String[] urlArray = ExtractLinks(extraText);


                if (urlArray[0] != null)
                {

                    // PsProcess[] processArray = new PsProcess[] { new PsProcess("xbyuri", "/checkuri", urlArray[0], null) };

                    // String url = MainActivity.DOMAIN + "/checkuri?p=" + JsonConvert.SerializeObject(processArray);


                    String url = MainActivity.DOMAIN + "/startprocess?name=xbyuri&inputValue=" + urlArray[0];


                    mWebView.LoadUrl(url);
                }

            }
            else if (Intent.ActionSend.Equals(action) && type != null && type.StartsWith("image/"))
            {

                // ein neues image wird erstellt... - ohne existierende prozessid

                // neu - nein, bei einem image intent laden wir die seite bewusst nicht!
                // denn diese seite stört immer den image upload!

                Boolean extraOk = true;

                Android.Net.Uri imageUri = (Android.Net.Uri)intent.GetParcelableExtra(Intent.ExtraStream);
                if (imageUri.ToString() == "" || imageUri.ToString() == null)
                {
                    extraOk = false;
                }


                String cookie = this.GetCookie(MainActivity.DOMAIN, "connect.sid");
                if (cookie == null)
                {
                    extraOk = false;
                }

                if (extraOk == true)
                {
                    mWebView.LoadUrl("about:blank");

                    Intent i = new Intent(this, typeof(ImgCreateActivity));

                    i.PutExtra("cookie", cookie);
                    i.PutExtra("imageUri", imageUri.ToString());


                    // start the process
                    // PsProcess[] processArray = new PsProcess[] { new PsProcess("image", "/x-create", null, null) };
                    // i.PutExtra("processArray", JsonConvert.SerializeObject(processArray));

                    this.StartActivityForResult(i, IMG_CREATE_REQUEST);
                }


            } else
            {


                // Load url to be rendered on WebView

                String path = intent.GetStringExtra("path");
                String url = MainActivity.DOMAIN;


                if (path != null)
                {
                    url += path;
                }

                if (Intent.ActionView.Equals(action))
                {
                    Android.Net.Uri data = intent.Data;
                    if (data != null)
                    {
                        url = data.ToString();
                    }
                }



                mWebView.LoadUrl(url);

            }

        }


        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {

 /*           if (requestCode == INPUT_FILE_REQUEST && mFilePathCallback != null)
            {
                Android.Net.Uri[] results = null;

                if (data != null)
                {
                    String dataString = data.DataString;
                    if (dataString != null)
                    {
                        results = new Android.Net.Uri[] { Android.Net.Uri.Parse(dataString) };
                    }
                }


                mFilePathCallback.OnReceiveValue(results);
                mFilePathCallback = null;
                return;
            }
*/

            base.OnActivityResult(requestCode, resultCode, data);


            if (requestCode == IMG_CREATE_REQUEST && resultCode == Result.Ok)
            {
                // String processArrayString = data.GetStringExtra("processArray");

                String newEntityId = data.GetStringExtra("newEntityId");
                String processId = data.GetStringExtra("processId");

                // PsProcess[] processArray = JsonConvert.DeserializeObject<PsProcess[]>(processArrayString);
                //String url = MainActivity.DOMAIN + processArray[0].path + "?p=" + processArrayString;

                String url = MainActivity.DOMAIN + "/startprocess?name=imagedescription&gxid=" + newEntityId;
                if (processId != null)
                {
                    // this is when a process was running, eg action started in app
                    url = MainActivity.DOMAIN + "/startprocess?p=" + processId + "&name=imagedescription&gxid=" + newEntityId;
                }


                mWebView.LoadUrl(url);

            }

            if (requestCode == IMG_CREATE_REQUEST && resultCode == Result.Canceled)
            {

                String url = MainActivity.DOMAIN;
                mWebView.LoadUrl(url);

            }


            return;

        }

        protected override void OnResume()
        {
            base.OnResume();
            RegisterReceiver(mBroadcasReceiver, new IntentFilter("android.net.conn.CONNECTIVITY_CHANGE"));
        }

        protected override void OnPause()
        {
            UnregisterReceiver(mBroadcasReceiver);
            base.OnPause();
        }


        public class MyWebChromeClient : WebChromeClient
        {
            private MainActivity _activity;

            public MyWebChromeClient(MainActivity activity)
            {
                _activity = activity;

            }

            // für den file upload
            // eigentlich nicht mehr relevant - habe ja den eigenen uploader gebaut
            /*
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
            }*/

            // Fuer geolocation - https://gist.github.com/Cheesebaron/ad84740c9bffa7e255c8
            /*public override void OnGeolocationPermissionsShowPrompt(string origin, GeolocationPermissions.ICallback callback)
            {
                const bool remember = false;
                var builder = new AlertDialog.Builder(_activity);
                builder.SetTitle("Location")
                    .SetMessage(string.Format("{0} would like to use your current location", origin))
                    .SetPositiveButton("Allow", (sender, args) => callback.Invoke(origin, true, remember))
                    .SetNegativeButton("Disallow", (sender, args) => callback.Invoke(origin, false, remember));
                var alert = builder.Create();
                alert.Show();
            } */

            // Ohne Alert
           public override void OnGeolocationPermissionsShowPrompt(String origin, GeolocationPermissions.ICallback callback)
            {

                Log.Info(TAG, "Geo Location Request. Check Permission");

                // also check for permission
                // see https://github.com/xamarin/monodroid-samples/blob/master/android-m/RuntimePermissions/MainActivity.cs
                const string fineLocationPermission = Android.Manifest.Permission.AccessFineLocation;

                if (ActivityCompat.CheckSelfPermission(_activity, fineLocationPermission) == (int)Permission.Granted)
                {
                    Log.Info(TAG, "FINE LOCATION already has been granted, grant permission to browser");
                    // fine location permission has been granted
                    callback.Invoke(origin, true, true);
                } else
                {
                    // fine location permission has not been yet granted
                    ActivityCompat.RequestPermissions(_activity, new String[] { fineLocationPermission }, PERMISSION_REQUEST_FINE_LOCATION);
                }



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

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == PERMISSION_REQUEST_FINE_LOCATION)
            {
                Log.Info(TAG, "Received response for FINE LOCATION permission request.");

                if (grantResults.Length == 1 && grantResults[0] == Permission.Granted)
                {
                    Log.Info(TAG, "FINE LOCATION permission has now been granted.");
                    Toast.MakeText(this, "Permission has now been granted. Try getting location again.", ToastLength.Short).Show();
                    // dies ist sicher nich optimal, ich weiss aber nicht, wie das callback zu handeln
                } else
                {
                    Log.Info(TAG, "FINE LOCATION permission was NOT granted.");

                    Toast.MakeText(this, "Permission was not granted", ToastLength.Short).Show();
                }

            } else
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
            
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


        public static String[] ExtractLinks(String text)
        {
            List<String> links = new List<String>();
            Matcher matcher = Patterns.WebUrl.Matcher(text);
            while (matcher.Find())
            {
                String url = matcher.Group();
                Log.Debug(TAG, "URL extracted: " + url);
                links.Add(url);
            }
            return links.ToArray();
        }

        //https://developer.android.com/guide/webapps/webview.html
        // https://developer.xamarin.com/recipes/android/controls/webview/call_csharp_from_javascript/

        public class WebAppInterface : Java.Lang.Object
        {

            private MainActivity mMainAcitivity;

            
            public WebAppInterface(MainActivity mainActivity)
            {
                this.mMainAcitivity = mainActivity;
            }

            /*
            [Export]
            [JavascriptInterface]
            public void CreateImg(String processArrayString)
            {

                // mMainAcitivity.processArray = JsonConvert.DeserializeObject<PsProcess[]>(processArrayString);
                // ich weiss einfach nicht, ob diese mMainActivty erhalten bleibt.. ich glaube eben nicht..


                List<PsProcess> processArray = JsonConvert.DeserializeObject<List<PsProcess>>(processArrayString);
                processArray.Insert(0, new PsProcess("image", "/x-create", null, null));
                processArrayString = JsonConvert.SerializeObject(processArray);





                String cookie = mMainAcitivity.GetCookie(MainActivity.DOMAIN, "connect.sid");
                if (cookie == null)
                {
                    return;
                }

                mMainAcitivity.RunOnUiThread(() => {
                    mMainAcitivity.mWebView.LoadUrl("about:blank");
                });


                Intent i = new Intent(mMainAcitivity, typeof(ImgCreateActivity));
                i.PutExtra("cookie", cookie);
                i.PutExtra("processArray", processArrayString);

                mMainAcitivity.StartActivityForResult(i, MainActivity.IMG_CREATE_REQUEST);

            } */

            [Export]
            [JavascriptInterface]
            public void CreateImg(String processId)
            {




                String cookie = mMainAcitivity.GetCookie(MainActivity.DOMAIN, "connect.sid");
                if (cookie == null)
                {
                    return;
                }

                mMainAcitivity.RunOnUiThread(() => {
                    mMainAcitivity.mWebView.LoadUrl("about:blank");
                });


                Intent i = new Intent(mMainAcitivity, typeof(ImgCreateActivity));
                i.PutExtra("cookie", cookie);
                i.PutExtra("processId", processId);

                mMainAcitivity.StartActivityForResult(i, MainActivity.IMG_CREATE_REQUEST);

            }

            [Export]
            [JavascriptInterface]
            public void Share(String text)
            {

                Intent shareIntent = new Intent(Intent.ActionSend);
                shareIntent.SetType("text/plain");
                shareIntent.PutExtra(Intent.ExtraText, text);
                mMainAcitivity.StartActivity(Intent.CreateChooser(shareIntent, "Teile mittels"));

            }
        }


        public String GetCookie(String siteName, String cookieName)
        {
            String cookieValue = null;

            String cookies = CookieManager.Instance.GetCookie(siteName);
            String[] cookieArray = cookies.Split(';');
            for (int i = 0; i < cookieArray.Length; i++)
            {
                String cookie = cookieArray[i];
                cookie = cookie.TrimStart(' ');
                String[] keyValue = cookie.Split('=');
                if ( keyValue[0].Equals(cookieName))
                { 
                    cookieValue = keyValue[1];
                    // simpler
                    cookieValue = cookie;
                    break;
                }
            }
            return cookieValue;
        }


        


    }


    public class PsProcess
    {
        public String _id { get; set; }
        public String name { get; set; }
        public String path { get; set; }
        public String inputValue { get; set; }
        public String gxid { get; set; }
        public String selectedType { get; set; }
        public String text { get; set; }
        public List<String> subjectId { get; set; }

        public PsProcess(String name, String path, String inputValue, String gxid)
        {
            this._id = Guid.NewGuid().ToString();
            this.name = name;
            this.path = path;
            this.inputValue = inputValue;
            this.gxid = gxid;
            
        }
    }




    public class MyBroadcastReceiver : BroadcastReceiver
    {

        private MainActivity _activity;

        public MyBroadcastReceiver(MainActivity activity)
        {
            _activity = activity;

        }

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Extras != null)
            {
                ConnectivityManager connectivityManager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
                NetworkInfo networkInfo = connectivityManager.ActiveNetworkInfo;

                if (networkInfo != null && networkInfo.IsConnectedOrConnecting)
                {
                    // network Available
                    if (_activity.mWebView.Url.Equals("file:///android_asset/offline.html") == true)
                    {
                        _activity.mWebView.GoBack();
                    }
                }
                else if (intent.GetBooleanExtra(ConnectivityManager.ExtraNoConnectivity, false))
                {
                    // network not Available
                    _activity.mWebView.LoadUrl("file:///android_asset/offline.html");

                }

            }
        }

    }



}

