"use strict";

$().ready(function () {

    // tab fragment logic
    $("button[data-tab-name]").each(function () {
        let element = $(this);
        const fragment = element.attr("data-tab-name");
        element.on("click", function () {
            window.location = "#" + fragment;
        })
    });
    const fragment = window.location.hash.substring(1);
    let tabElement = $("button[data-tab-name='" + fragment + "']");
    if (tabElement.length)
        tabElement.trigger("click");
});