rerun();

function rerun()
{
    let port = chrome.runtime.connectNative('unpack_zip');
    port.onMessage.addListener(message =>
    {
        if (typeof(message) == "string")
        {
            console.log(message);
        }
    })

    chrome.downloads.onChanged.addListener(download =>
    {
        if (!download.state)
        {
            return;
        }
        if (download.state.current !== "complete")
        {
            return;
        }
        chrome.downloads.search({
            id: download.id,
        }).then(x =>
        {
            if (x.length === 0)
            {
                return;
            }

            port.postMessage({
                filePath: x[0].filename,
            });
        });
    });
}
