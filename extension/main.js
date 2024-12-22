let port = null;
rerun();
document.getElementById("rerun").onclick(rerun);

function rerun()
{
    if (port != null)
    {
        port.close();
        port = null;
    }

    port = chrome.runtime.connectNative('unpack-zip');
    port.onEachMessage(message => {
        let div = document.getElementById("errors");
        let row = document.createElement("div");
        row.textContent = message;
        div.append(row);
    })

    port.postMessage({filePath: "C:\\Users\\Anton\\Downloads\\folder\\ghoul.zip"});
}