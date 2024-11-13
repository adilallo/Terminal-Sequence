var FlickrPlugin = {
    FetchFlickrImagesJS: function(searchTextPtr, page) {
        var searchText = UTF8ToString(searchTextPtr);

        var apiKey = '976c8648744b8e40c8541aba9ed2f978';
        var cacheBuster = Math.floor(Math.random() * 10000).toString();
        var url = 'https://www.flickr.com/services/rest/?' +
                  'method=flickr.photos.search' +
                  '&api_key=' + apiKey +
                  '&text=' + encodeURIComponent(searchText) +
                  '&format=json' +
                  '&nojsoncallback=1' +
                  '&per_page=50' +
                  '&page=' + page +
                  '&cachebuster=' + cacheBuster;

        // Create a script element for JSONP
        var script = document.createElement('script');
        var callbackName = 'handleFlickrResponse_' + Math.floor(Math.random() * 1000000);

        // Define the callback function
        window[callbackName] = function(response) {
            // Send data to Unity
            var jsonData = JSON.stringify(response);
            SendMessage('FlickrImageLoader', 'OnFlickrResponse', jsonData);

            // Clean up
            delete window[callbackName];
            document.head.removeChild(script);
        };

        url += '&jsoncallback=' + callbackName;
        script.src = url;
        document.head.appendChild(script);
    }
};

mergeInto(LibraryManager.library, FlickrPlugin);
