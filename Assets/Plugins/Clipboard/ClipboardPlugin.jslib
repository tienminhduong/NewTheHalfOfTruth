mergeInto(LibraryManager.library, {

    CopyToClipboard: function (textPtr) {
        // Convert the C# string pointer to a JavaScript string
        var textToCopy = UTF8ToString(textPtr);

        // Check if the browser supports the modern Clipboard API
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(textToCopy).then(function() {
                console.log('Text successfully copied to clipboard');
            }).catch(function(err) {
                console.error('Could not copy text: ', err);
            });
        } else {
            console.warn('Clipboard API not supported in this browser.');
        }
    }

});
