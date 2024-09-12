var dataTable;
$(document).ready(function () {
    var url = window.location.search;

    if (url.includes("inprocess")) {
        console.log("Loading in-process orders");
        LoadDataTable("inprocess");
    } else if (url.includes("completed")) {
        console.log("Loading completed orders");
        LoadDataTable("completed");
    } else if (url.includes("paymentpending")) {
        console.log("Loading payment-pending orders");
        LoadDataTable("paymentpending");
    } else if (url.includes("approved")) {
        console.log("Loading approved orders");
        LoadDataTable("approved");
    } else {
        console.log("Loading all orders");
        LoadDataTable("all");
    }
});

function LoadDataTable(status) {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/order/getall?status=' + status },
        "columns": [
            { data: 'orderHeader_Id', 'width': "25%" },
            { data: 'name', 'width': "10%" },
            { data: 'phoneNumber', 'width': "10%" },
            { data: 'applicationUser.email', 'width': "15%" },
            { data: 'orderStatus', 'width': "10%" },
            { data: 'orderTotal', 'width': "10%" },
            {
                data: 'orderHeader_Id',
                "render": function (data) {
                    return `<div class="w-75 btn-group" role="group"> 
                        <a href="/admin/Order/detail?orderId=${data}" class="btn btn-primary mx-2"> <i class="bi bi-pencil-square"></i></a>
                    </div>`;
                },
                "width": "25%"
            }
        ]
    });
}
