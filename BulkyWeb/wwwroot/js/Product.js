var dataTable;
$(document).ready(function () {
    LoadDataTable();
});
function LoadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/product/getall' },
        "columns": [
            { data: 'title', 'width': "25%" },
            { data: 'isbn', 'width': "10%" },
            { data: 'listPrice', 'width': "10%" },
            { data: 'author', 'width': "15%" },
            { data: 'category.name', 'width': "10%" },
            {
                data: 'product_id',
                "render": function (data) {
                    return `<div class="w-75 btn-group" role="group"> 
                        <a href="/admin/product/upsert?id=${data}" class="btn btn-primary mx-2"> <i class="bi bi-pencil-square"></i>Edit</a> 
                        <a onClick="OnDeleteAlert(${data})" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i>Delete</a>
                    </div>`
                },
                "width": "25%"
            }
        ]
    });
}
function OnDeleteAlert(id) {
    Swal.fire({
        title: "Are you sure?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: `/admin/product/delete?id=${id}`,
                type: "DELETE",
                success: function (data) {
                    if (data) {
                        Swal.fire({
                            title: "Deleted!",
                            text: "Your file has been deleted.",
                            icon: "success"
                        });
                        dataTable.ajax.reload();
                    }
                    else {
                        Swal.fire({
                            title: "Error!",
                            text: "There was an issue deleting the file.",
                            icon: "error"
                        });
                    }
                },
                error: function (error) {
                    Swal.fire({
                        title: "Error!",
                        text: "There was an issue deleting the file.",
                        icon: "error"
                    });
                }
            })
        }
    });
}
