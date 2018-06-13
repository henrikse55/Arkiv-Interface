var IndexApp = new Vue({
    el: '#IndexApp',
    data: {
        selectedItem: '',
        filters: [],
        order: 'Ascending',
        colName: ''
    },
    methods: {
        ready: function () {
            $.ajax({
                type: 'POST',
                url: '/Archive/GetTable/',
                //data: { Filters: [{ Name: 'NONE', Type: 'Single', Value: { One: '5000', Two: null }}], OrderData: { Order: 'Ascending', Column: '' } },
                success: (data) => {
                    $('#TableContainer').html(data);
                    $('#loader').addClass("hidden");
                },
                error: (jqXHR, exception) => {
                    $('#TableContainer').html(`<h1 style="color: red;">${exception} - ${jqXHR.status}</h1>`); //If an error occures, display an error message
                }
            });
        },
        GetSelectedItem: function () {
            let Item = this.$data.selectedItem;
            this.filters.push(Item);
        },
        PostFilters: function () {
            if($('.FilterGroup').length == 0) {
                Snack('The system was not able to find any matching record(s)!', '#e60000');

                return;
            }

            $('#ApplyButton').attr('disabled', true); //Disable ApplyButton while the data is being processed
            $('#ProgressBar').css({ 'visibility': 'visible', 'width': '100%' });

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
            console.log({ Order: this.order, Column: this.colName });

            $.ajax({
                type: 'POST',
                url: '/Archive/GetTable/',
                data: { Filters: FinalFilters, OrderData: { Order: this.order, Column: this.colName }},
                success: (data) => {
                    if (data !== 'No Match') {
                        $('#TableContainer').html('');
                        $('#TableContainer').html(data);

                        Snack('The requested record(s) was found successfuly!', '#33cc33');

                        $('#ApplyButton').removeAttr('disabled'); //Re-enable the Apply button
                        $('#ProgressBar').css({ 'visibility': 'hidden', 'width': '100%' });
                    } else {
                        Snack('The system was not able to find any matching record(s)!', '#e60000');

                        $('#ApplyButton').removeAttr('disabled'); //Re-enable the Apply button
                        $('#ProgressBar').css({ 'visibility': 'hidden', 'width': '100%' });
                    }
                },
                error: (jqXHR, exception) => {
                    $('#TableContainer').html(`<h1 style="color: red;">${exception} - ${jqXHR.status}</h1>`); //If an error occures, display an error message
                }
            });
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
            order: 'Ascending'
        }
    },
    template: '<select v-model="order" class="form-control" id="OrderSelect"><option value="Ascending">Ascending</option><option value="Descending">Descending</option></select>',
    watch: {
        order: {
            handler: function () {
                IndexApp.order = this.order;
            }
        }
    }
});

IndexApp.ready();


function Snack(message, color) {
    Snackbar.show({
        text: message,
        pos: 'top-left',
        backgroundColor: color,
        showAction: false
    });
}