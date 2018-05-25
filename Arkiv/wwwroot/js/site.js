//Used in: Archive/Index
function GetSelectedItem(option)
{
    console.log(option.value);

    if ($('#FilterPartial' + option.value).length === 0)
    {
        $.ajax(
        {
            url: '/Archive/GetFilterPartial/',
            cache: false,
            type: 'POST',
            data: { SelectedColumn: option.value },
            success: (data) => {
                $('#FilterContainer').append(data);
                $('#FilterContainer').fadeIn('slow');
            },
            error: (response) => {
                alert('Error: ' + response);
            }
        });
    }
}

//Used in: Archive/FilterPartial
function RemoveFilter(FilterName)
{
    $(FilterName).remove(); //Remove specified element
}