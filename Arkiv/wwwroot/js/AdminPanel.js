var admin = new Vue({
    el: "#adminpanel",
    data: {
        loading: {
            groups: true,
            log: true,
        },
        groups: [],
        logs: [],
        model: {
            ADR: {}
        }
    },
    methods: {
        init: function () {
            $.get("/api/active/List", (data) => {
                this.groups = data.groups;
                this.logs = data.logs;
                $('#ADRTable').removeClass("loading");
                $('#logTable').removeClass("loading");
                this.loading.groups = false
            });
        },
        DeleteADE: function (id)
        {
            $.post("/api/active/deleteADE?id=" + id, (data) =>
            {
                if (data.success) {
                    let temp = [];
                    for (let i = 0; i < this.groups.length; i++)
                    {
                        if (this.groups[i].id != id) {
                            temp.push(this.groups[i]);
                        }
                    }
                    this.groups = temp;
                } else {
                    console.log("Something unexpected happened, please try again later");
                }
            });
        },
        AddADE: function () {
            $.post("/api/active/addEntry", this.model.ADR, (data) => {
                if (data != null) {
                    this.groups.push(data);
                }
            });
        },
    }
});

admin.init();