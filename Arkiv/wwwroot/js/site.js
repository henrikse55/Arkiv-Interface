var IndexApp = new Vue({
    el: '#IndexApp',
    data: {
        tableLoader: true,
        selectedItem: '',
        filters: [],
        pages: 0,
        page: {
            pages: 0,
            numbers: [],
            current: 0
        },
        orderBy: 'Ascending',
        colName: 'Id',
        PagingBarDisabled: false
    },
    methods: {
        ready: function () {
            $.ajax({
                type: 'POST',
                url: '/Archive/GetTable/',
                success: (data) => {
                    $('#TableContainer').html(data);
                    this.tableLoader = false;
                    this.CalculatePages();
                },
                error: (jqXHR, exception) => {
                    $('#TableContainer').html(`<h1 style="color: red;">${exception} - ${jqXHR.status}</h1>`); //If an error occures, display an error message
                }
            });
        },
        GetSelectedItem: function () {
            let Item = this.selectedItem;
            this.filters.push(Item);
        },
        PostFilters: function (isPageChange) {
            if (!isPageChange && $('.FilterGroup').length == 0) {
                Snack('You must add a filter in order to make a search!', '#e60000');

                return;
            }

            let Filters = [];

            let ValidationError = false; //Indicates if an element contains a validation error

            //Iterate through each element in the FilterGroup class
            $('.FilterGroup').map((i, e) => {
                let col = e.id.split('_');

                //Value validation of current element
                if (e.value == '') {
                    Snack('Validation error: ' + col[1] + ' can not be empty!', '#e60000');
                    $('#ApplyButton').removeAttr('disabled'); //Re-enable the Apply button
                    $('#ProgressBar').css({ 'visibility': 'hidden', 'width': '100%' });

                    ValidationError = true;
                    
                    return;
                }


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

            //If a validation error has been found, then exit method
            if (ValidationError) {
                return;
            }

            this.tableLoader = true;

            $('#ApplyButton').attr('disabled', true); //Disable ApplyButton while the data is being processed
            $('#ProgressBar').css({ 'visibility': 'visible', 'width': '100%' });

            if (isPageChange) {
                this.PagingBarDisabled = true;
            }

            $('#paging').attr('disabled', true); //Disable ApplyButton while the data is being processed

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

            $('#TableContainer').html("");

            $.ajax({
                type: 'POST',
                url: '/Archive/GetTable/',
                data: { Filters: FinalFilters, OrderData: { Order: this.orderBy, Column: this.colName }, pages: this.page.current},
                success: (data) => {
                    if (data !== 'No Match') {
                        $('#TableContainer').html(data);

                        Snack('The requested record(s) was found successfuly!', '#33cc33');

                        $('#ApplyButton').removeAttr('disabled'); //Re-enable the Apply button
                        $('#ProgressBar').css({ 'visibility': 'hidden', 'width': '100%' });
                    } else {
                        Snack('The system was not able to find any matching record(s)!', '#e60000');

                        $('#ApplyButton').removeAttr('disabled'); //Re-enable the Apply button
                        $('#ProgressBar').css({ 'visibility': 'hidden', 'width': '100%' });
                    }

                    this.PagingBarDisabled = false;
                    this.tableLoader = false;
                },
                error: (jqXHR, exception) => {
                    this.tableLoader = false;
                    $('#TableContainer').html(`<h1 style="color: red;">${exception} - ${jqXHR.status}</h1>`); //If an error occures, display an error message
                }
            });
        },
        CalculatePages: function ()
        {
            this.page.pages = parseInt($("#pages").html());

            this.page.numbers = [];
            let start = (this.page.current - 5);
            for (let i = start >= 0 ? start : 0; i < this.page.current; i++)
            {
                this.page.numbers.push({item: i, active: ""});
            }

            this.page.numbers.push({ item: this.page.current, active: "active" });
            for (let i = this.page.current + 1; i < this.page.current + 6; i++) {
                if (i <= this.page.pages)
                    this.page.numbers.push({ item: i, active: "" });
            }
        },
        PageChange: function (page)
        {
            this.page.current = page;
            this.CalculatePages();
            this.PostFilters(true);
        }
    }
});

Vue.component('filter-template', {
    props: ['name'],
    data: function() {
        return {
            option: 'Single',
            orderShow: false,
            orderSelect: ""

        }
    },
    template: '#filter-template',
    methods: {
        RemoveFilter: function (FilterName) {
            $("#FilterPartial" + FilterName).remove(); //Remove specified element
            let filters = [];
            for (let i = 0; i < IndexApp.filters.length; i++)
            {
                if (IndexApp.filters[i] != FilterName)
                    filters.push(IndexApp.filters[i]);
            }

            IndexApp.filters = filters;
        },
        ChangeTextInputType: function(col) {
            $('#TextInputContainer' + col).html('');

            let single = `<input type="text" class="form-control FilterGroup" id="Filtering_${col}" />`;
            let range = `<input type="text" class="form-control FilterGroup" id="FilteringFrom_${col}" /> - <input type="text" class="form-control FilterGroup" id="FilteringTo_${col}" />`;

            switch (this.option) {
                case 'Single':
                    $('#TextInputContainer' + col).html(single);
                    break;
                case 'Range':
                    $('#TextInputContainer' + col).html(range);
                    break;
            }
        },
        AddAsDeOptions: function (name) {
            if (this.orderSelect == "" && $('#OrderSelect').length == 0) {
                this.orderShow = true;
                this.orderSelect = name;
                IndexApp.colName = name;
            }
        },
        RemoveAsDeOptions: function () {
            this.orderShow = false;
            this.orderSelect = "";
            IndexApp.colName = '';
        },
    }
});

Vue.component('order-select', {
    data: function() {
        return {
            orderBy: 'Ascending'
        }
    },
    template: '<select v-model="orderBy" class="form-control" id="OrderSelect"><option value="Ascending">Ascending</option><option value="Descending">Descending</option></select>',
    watch: {
        orderBy: {
            handler: function () {
                IndexApp.orderBy = this.orderBy;
            }
        }
    }
});

IndexApp.ready();

//This method uses the snackbar.js library
function Snack(message, color) {
    Snackbar.show({
        text: message,
        pos: 'top-left',
        backgroundColor: color,
        showAction: false
    });
}