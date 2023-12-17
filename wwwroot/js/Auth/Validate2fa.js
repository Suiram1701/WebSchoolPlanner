"use strict";

$().ready(function () {

    // Send the form if enough letters are given
    let inputElement = $("input[id='codeInput']");
    const requiredLetters = parseInt(inputElement.attr("data-digit-count"));
    inputElement.on("input", function () {
        const content = inputElement.val().replace(" ", "");     // Remove spaces
        if (content.length >= requiredLetters) {
            $("form[id='codeForm']").submit();
        }
    });

    // Remove invalid mark on input
    let form = $("form[id='codeForm']");
    if (form.hasClass("form-invalid")) {     // Use only when input invalid
        $("input[type='text']").each(function () {
            $(this).on("input", function () {
                form.removeClass("form-invalid");
            });
        });
    }

    // Two factor method switch
    let messages = JSON.parse($("#messages").text());
    $("button[data-method]").each(function () {
        let element = $(this);
        const method = element.attr("data-method");
        const placeholder = messages.placeholder[method];
        element.click(function () {
            $("#methodField").attr("value", method);
            $("label[for='codeInput']").text(placeholder)

            // Only when email two factor available
            let codeBtn = $("#sendCode");
            if (codeBtn == undefined)
                return;

            if (method === "Email")
                codeBtn.prop("hidden", false);
            else
                codeBtn.prop("hidden", true);
        });
    });

    // 2fa email confirmation
    let codeBtn = $("#sendCode");
    const newCodeLabel = codeBtn.attr("data-codeLabel");
    codeBtn.click(function () {
        codeBtn.text(newCodeLabel);

        $.ajax({
            type: "GET",
            url: "/auth/2fa/email",
            error: function (data) {     // Always executed

                // Error
                if (data.status !== 200) {
                    LogAjaxError(data);
                    ShowAjaxErrorBox(data);
                    return;
                }
            }
        });
    });
});