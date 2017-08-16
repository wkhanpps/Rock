<%@ WebHandler Language="C#" Class="TextToWorkflowTwilioAsync" %>

using System;
using System.Web;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Rock;
using Rock.Model;
using Rock.Web.Cache;
using com.minecartstudio.TextToWorkflow;


public class TextToWorkflowTwilioAsync : IHttpAsyncHandler
{
    private HttpRequest request;
    private HttpResponse response;

    public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, Object extraData)
    {
        TextToWorkflowReponseAsync twilioAsync = new TextToWorkflowReponseAsync(cb, context, extraData);
        twilioAsync.StartAsyncWork();
        return twilioAsync;
    }

    public void EndProcessRequest(IAsyncResult result)
    {
    }

    public void ProcessRequest(HttpContext context)
    {
        throw new InvalidOperationException();
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }
}

class TextToWorkflowReponseAsync : IAsyncResult
{
    private bool _completed;
    private Object _state;
    private AsyncCallback _callback;
    private HttpContext _context;

    bool IAsyncResult.IsCompleted { get { return _completed; } }
    WaitHandle IAsyncResult.AsyncWaitHandle { get { return null; } }
    Object IAsyncResult.AsyncState { get { return _state; } }
    bool IAsyncResult.CompletedSynchronously { get { return false; } }

    public TextToWorkflowReponseAsync(AsyncCallback callback, HttpContext context, Object state)
    {
        _callback = callback;
        _context = context;
        _state = state;
        _completed = false;
    }

    public void StartAsyncWork()
    {
        ThreadPool.QueueUserWorkItem(new WaitCallback(StartAsyncTask), null);
    }

    private void StartAsyncTask(Object workItemState)
    {
        var request = _context.Request;
        var response = _context.Response;

        response.ContentType = "text/plain";

        if ( request.HttpMethod != "POST" )
        {
            response.Write( "Invalid request type. Please use POST." );
        }
        else {
            if ( request.Form["SmsStatus"] != null )
            {
                switch ( request.Form["SmsStatus"] )
                {
                    case "received":
                        string fromPhone = string.Empty;
                        string toPhone = string.Empty;
                        string message = string.Empty;


                        if ( !string.IsNullOrEmpty( request.Form["To"] ) )
                        {
                            toPhone = request.Form["To"];
                        }

                        if ( !string.IsNullOrEmpty( request.Form["From"] ) )
                        {
                            fromPhone = request.Form["From"];
                        }

                        if ( !string.IsNullOrEmpty( request.Form["Body"] ) )
                        {
                            message = request.Form["Body"];
                        }

                        string processResponse = string.Empty;

                        TextToWorkflowUtility.MessageRecieved( toPhone, fromPhone, message, out processResponse );

                        if ( processResponse != string.Empty )
                        {
                            response.Write( processResponse );
                        }

                        break;
                }
            }
            else
            {
                response.Write( "Proper input was not provided." );
            }
        }


        _completed = true;
        _callback(this);
    }
}