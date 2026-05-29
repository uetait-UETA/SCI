<%@ Page Language="C#" AutoEventWireup="true" CodeFile="DisTransferDetails.aspx.cs" Inherits="DisTransferDetails" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Dispatch/Received Document </title>
    <link href="default.css" rel="stylesheet" type="text/css" />
    <style>
        tr td {font-size: 8pt }
    </style>
</head>
<body>
    <form id="form1" runat="server">
   <div>
    
        <input type="button" onclick="window.print()" value="Print" style="height:22px; width:70px; font-size:8pt; font-weight:bold;" id="Button1" />
    
        
            <div id="divContent" runat="server" style="width:600px;"></div>

    
    </div>
    </form>
</body>
</html>
