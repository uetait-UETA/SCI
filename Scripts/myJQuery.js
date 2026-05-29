(function ($) {

    $('#divMessage').fadeIn('fast').delay(5000).fadeOut('fast');

    //$('input:radio[name="rbAddItems"]').change(function () {
    //    if ($(this).val() == 0) {
    //        $("#divItemHierarchy").hide();
    //        $("#divSingleItem").show();
    //        $("#divItemList").hide();
    //    }
    //    else if ($(this).val() == 1) {
    //        $("#divSingleItem").hide();
    //        $("#divItemHierarchy").hide();
    //        $("#divItemList").show();
    //    }
    //    else {
    //        $("#divSingleItem").hide();
    //        $("#divItemHierarchy").show();
    //        $("#divItemList").hide();
    //    }
    //});

    //if ($('#hdnItemSelection').val() == 0) {
    //        $('#rbSingleItem').prop('checked', true);
    //        $("#divItemHierarchy").hide();
    //        $("#divSingleItem").show();
    //        $("#divItemList").hide();
    //    }
    //else if ($('#hdnItemSelection').val() == 1) {
    //        $('#rbItemList').prop('checked', true);
    //        $("#divSingleItem").hide();
    //        $("#divItemHierarchy").hide();
    //        $("#divItemList").show();
    //    }
    //else {
    //        $('#rbItemHierarchy').prop('checked', true);
    //        $("#divSingleItem").hide();
    //        $("#divItemHierarchy").show();
    //        $("#divItemList").hide();
    //}

    //alert($('#popover').val());

    //$("input[id*='rbtnAddComponent']").click(function (evt) {
    //    var promoName = $("input[id*='rtbPromoName']").val();
    //    var promoDesc = $("input[id*='rtbPromoDesc']").val();
    //    var promoEvents = $("input[id*='rcbEvents']").val();
    //    var promoStartDate = $("input[id*='rdpStartDate']").val();
    //    var promoEndDate = $("input[id*='rdpEndDate']").val();
    //    //,"background": "#FFCECE"

    //    var stDate = new Date(promoStartDate);
    //    var enDate = new Date(promoEndDate);
    //    var curDay = new Date();

    //    if (promoName == "" || promoDesc == "" || promoEvents == "Select an Event" || promoStartDate == "" || promoEndDate == "" || stDate > enDate || stDate <= curDay || enDate <= curDay) {
    //        if (promoName == null || promoName == "") {
    //            $("input[id*='rtbPromoName']").attr("placeholder", "Enter Promo Name");
    //            $("input[id*='rtbPromoName']").css({
    //                "border": "1px solid red"
    //            });
    //        }
    //        if (promoDesc == null || promoDesc == "") {
    //            $("input[id*='rtbPromoDesc']").attr("placeholder", "Enter Promo Description");
    //            $("input[id*='rtbPromoDesc']").css({
    //                "border": "1px solid red"
    //            });
    //        }
    //        if (promoEvents == "Select an Event") {
    //            var eventID = $("input[id*='rcbEvents']").parent().attr("id");
    //            $("#" + eventID).css({
    //                "border": "1px solid red"
    //            });
    //        }
    //        if (promoStartDate == null || promoStartDate == "") {
    //            $("input[id*='rdpStartDate']").attr("placeholder", "Enter Start Date");
    //            $("input[id*='rdpStartDate']").css({
    //                "border": "1px solid red"
    //            });
    //        }
    //        if (promoEndDate == null || promoEndDate == "") {
    //            $("input[id*='rdpEndDate']").attr("placeholder", "Enter End Date");
    //            $("input[id*='rdpEndDate']").css({
    //                "border": "1px solid red"
    //            });
    //        }
    //        if (stDate <= curDay) {
    //            $("input[id*='rdpStartDate']").val("");
    //            $("input[id*='rdpStartDate']").attr("placeholder", "Should be > Today");
    //            $("input[id*='rdpStartDate']").css({
    //                "border": "1px solid red"
    //            });
    //        }
    //        if (enDate <= curDay) {
    //            $("input[id*='rdpEndDate']").val("");
    //            $("input[id*='rdpEndDate']").attr("placeholder", "Should be > Today");
    //            $("input[id*='rdpEndDate']").css({
    //                "border": "1px solid red"
    //            });
    //        }
    //        if (stDate <= curDay) {
    //            $("input[id*='rdpStartDate']").val("");
    //            $("input[id*='rdpStartDate']").attr("placeholder", "Should be > Today");
    //            $("input[id*='rdpStartDate']").css({
    //                "border": "1px solid red"
    //            });
    //        }
    //        if (enDate <= curDay) {
    //            $("input[id*='rdpEndDate']").val("");
    //            $("input[id*='rdpEndDate']").attr("placeholder", "Should be > Today");
    //            $("input[id*='rdpEndDate']").css({
    //                "border": "1px solid red"
    //            });
    //        }
    //        if (stDate > enDate) {
    //            $("input[id*='rdpStartDate']").val("");
    //            $("input[id*='rdpStartDate']").attr("placeholder", "Should be < End Date");
    //            $("input[id*='rdpStartDate']").css({
    //                "border": "1px solid red"
    //            });
    //        }
    //        return false;
    //    }
    //    //var nodes = document.getElementById("divPromoHeader").getElementsByTagName('*');
    //    //for (var i = 0; i < nodes.length; i++) {
    //    //    nodes[i].disabled = true;
    //    //}
    //    //$('#divPromoHeader').attr("disabled", "disabled").off('click');
    //    //$('#divPromoHeader').children().prop('disabled', true);
    //    //$('#divPromoHeader').children().off('click');
    //    //var combo = $find("<%=rtbPromoName.ClientID%>");
    //    //$("input[id*='rtbPromoName']").attr("disabled", true);
    //    //combo.disable();
    //    //$("#divPromoHeader").children().prop('disabled', 'disabled');
    //    //$("#divPromoHeader").prop('readonly', true);
    //    return true;
    //});

})(jQuery);

//function togglePopOver() {
//    $("input[id*='rtbPromoName']").popover({
//        placement: 'right',
//        content: "Test"
//    });
//};

//function ItemRadioChanged() {
//    if ($('#rbSingleItem').is(':checked'))
//    {
//        $("#divItemHierarchy").hide();
//        $("#divSingleItem").show();
//        $("#divItemList").hide();

//        $('#hdnItemSelection').val(0);
//    }

//    if ($('#rbItemList').is(':checked')) {
//        $("#divSingleItem").hide();
//        $("#divItemHierarchy").hide();
//        $("#divItemList").show();

//        $('#hdnItemSelection').val(1);
//    }
    
//    if ($('#rbItemHierarchy').is(':checked')) {
//        $("#divSingleItem").hide();
//        $("#divItemHierarchy").show();
//        $("#divItemList").hide();

//        $('#hdnItemSelection').val(2);
//    }

//    $("input[name=RowRemove]").each(function () {
//        $(this).click();
//    });
//};

//function ValidatePromoHeader() {
//    var promoName = $("input[id*='rtbPromoName']").val();
//    var promoDesc = $("input[id*='rtbPromoDesc']").val();
//    var promoEvents = $("input[id*='rcbEvents']").val();
//    var promoStartDate = $("input[id*='rdpStartDate']").val();
//    var promoEndDate = $("input[id*='rdpEndDate']").val();

//    if (promoName == null || promoName == "")
//    {
//        $("input[id*='rtbPromoName']").attr("placeholder", "Enter Promo Name");
//        return false;
//    }
//    return true;
//    //alert("Promo Name: " + promoName + "; Promo Desc: " + promoDesc + "; Event: " + promoEvents + "; Promo Start Date: " + promoStartDate + "; Promo End Date: " + promoEndDate);

//};