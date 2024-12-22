rerun();

function rerun()
{
    let port = chrome.runtime.connectNative('unpack_zip');
    port.onMessage.addListener(/** @param {string} message */ message =>
    {
        // let div = document.getElementById("errors");
        // let row = document.createElement("div");
        // row.textContent = message;
        // div.append(row);
        console.log(message);
        rerun();
    })

    port.postMessage({filePath: "C:\\Users\\Anton\\Downloads\\folder\\ghoul.zip"});
}
