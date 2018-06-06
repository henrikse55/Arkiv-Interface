var IndexApp = new Vue({
    el: '#IndexApp',
    data: {
        selectedItem: ''
    },
    methods: {
        GetSelectedItem: function () {
            let Item = this.$data.selectedItem;
            
            if (Item !== '') {
                $.post('/Archive/GetFilterPartial/', { SelectedColumn: Item }, (data) => { $('#FilterContainer').append(data) })
            }
        },
        PostFilters: function ()     {
            $('#ApplyButton').attr('disabled', true); //Disable ApplyButton while the data is being processed
            $('#ProgressBar').css({ 'visibility': 'visible', 'width': '100%' });

            let Filters = [];

            //Iterate through each element in the FilterGroup class
            $('.FilterGroup').map((i, e) => {
                let col = e.id.split('_');

                //Check if the id FilteringFrom_ exists. If it does,
                //the current element is part of a range filter.
                if ($('#FilteringFrom_' + col[1]).length === 1) {
                    Filters.push({
                        Column: {
                            Name: col[1],
                            Type: col[0] === 'FilteringFrom' ? 'From' : 'To',
                            Value: e.value
                        }
                    });
                } else {
                    Filters.push({
                        Column: {
                            Name: col[1],
                            Type: 'Single',
                            Value: e.value
                        }
                    });
                }
            });

            let FinalFilters = [];

            let TempFrom = []; //Used for temporarily storing the 'From' part of a range filter
            let RangeMode = false;

            //Convert the filters into an array of json objects
            for (let i = 0; i < Filters.length; i++) {
                if (RangeMode) {
                    RangeMode = false;

                    FinalFilters.push({
                        Name: Filters[i].Column.Name,
                        Type: 'Range',
                        Value: {
                            One: TempFrom[0].Value,
                            Two: Filters[i].Column.Value
                        }
                    });

                    TempFrom = [];
                }

                switch (Filters[i].Column.Type) {
                    case 'From':
                        RangeMode = true;
                        TempFrom.push(Filters[i].Column);
                        break;
                    case 'Single':
                        FinalFilters.push({
                            Name: Filters[i].Column.Name,
                            Type: 'Single',
                            Value: {
                                One: Filters[i].Column.Value,
                                Two: null
                            }
                        });
                        break;
                }
            }

            console.log(FinalFilters);

            $.post('/Archive/GetTable/', { Filters: FinalFilters }, (data) => {
                if (data !== 'No Match') {
                    $('#TableContainer').html('');
                    $('#TableContainer').html(data);

                    Snackbar.show({
                        text: 'The requested record(s) was found successfuly!',
                        pos: 'top-left',
                        backgroundColor: '#33cc33',
                        showAction: false
                    });
                    $('#ApplyButton').removeAttr('disabled'); //Re-enable the Apply button
                    $('#ProgressBar').css({ 'visibility': 'hidden', 'width': '100%' });
                } else {
                    Snackbar.show({
                        text: 'The system was not able to find any matching record(s)!',
                        pos: 'top-left',
                        backgroundColor: '#e60000',
                        showAction: false
                    });

                    $('#ApplyButton').removeAttr('disabled'); //Re-enable the Apply button
                    $('#ProgressBar').css({ 'visibility': 'hidden', 'width': '100%' });
                }
            });
        }
    }
});

//Used in: Archive/FilterPartial
function RemoveFilter(FilterName)
{
    $(FilterName).remove(); //Remove specified element
}

//Used in: Archive/FilterPartial
function ChangeTextInputType(option, col)
{
    $('#TextInputContainer' + col).html('');

    let single = `<input type="text" class="form-control FilterGroup" id="Filtering_${col}" />`;
    let range = `<input type="text" class="form-control FilterGroup" id="FilteringFrom_${col}" /> - <input type="text" class="form-control FilterGroup" id="FilteringTo_${col}" />`;

    switch (option.value)
    {
        case 'Single':
            $('#TextInputContainer' + col).html(single);
            break;
        case 'Range':
            $('#TextInputContainer' + col).html(range);
            break;
    }
}