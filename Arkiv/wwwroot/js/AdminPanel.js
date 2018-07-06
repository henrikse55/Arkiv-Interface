var admin = new Vue({
    el: "#adminpanel",
    data: {
        //Some State Information
        loading: {
            groups: true,
            log: true,
            blacklist: true,
        },

        page: {
            numbers: [],
            current: 0,
            count: 0,
        },
        pageBlacklist: {
            numbers: [],
            current: 0,
            count: 0,
        },

        //Table information
        groups: [],
        logs: [],
        blacklist: [],

        //bound models
        model: {
            ADR: {},
            blacklist: "",
        },
        clickedParam: ''
    },
    methods: {
        init: function () {
            $.get("/api/active/list", (data) => {
                this.groups = data.groups;
                $('#ADRTable').removeClass("loading");
                this.loading.groups = false
            });

            this.loading.blacklist = false;
            this.PageChange(0);
            this.PageChangeBlacklist(0);
        },
        PageChange: function (offset)
        {
            this.page.current = offset;

            $.get("/api/active/logs", { offset: this.page.current * 25 }, (data) => {
                this.page.count = (data.count / 25);
                this.logs = data.logs;
                $('#logTable').removeClass("loading");
                this.loading.log = false;
            }).then(data => {
                this.page.numbers = [];
                let start = (this.page.current - 5);
                for (let i = start >= 0 ? start : 0; i < this.page.current; i++) {
                    this.page.numbers.push({ item: i, active: "" });
                }

                this.page.numbers.push({ item: this.page.current, active: "active" });

                for (let i = this.page.current + 1; i < this.page.current + 6; i++) {
                    if (i <= this.page.count)
                        this.page.numbers.push({ item: i, active: "" });
                }
            });


        },

        PageChangeBlacklist: function (offset) {
            this.pageBlacklist.current = offset;

            $.get("/api/active/blacklist", { offset: this.pageBlacklist.current * 25 }, (data) => {
                this.pageBlacklist.count = (data.count / 25);
                this.blacklist = data.blacklist;
                $('#columnBlacklist').removeClass("loading");
                this.loading.blacklist = false;
            }).then(data => {
                this.pageBlacklist.numbers = [];
                let start = (this.pageBlacklist.current - 5);
                for (let i = start >= 0 ? start : 0; i < this.pageBlacklist.current; i++) {
                    this.pageBlacklist.numbers.push({ item: i, active: "" });
                }

                this.pageBlacklist.numbers.push({ item: this.pageBlacklist.current, active: "active" });

                for (let i = this.pageBlacklist.current + 1; i < this.pageBlacklist.current + 6; i++) {
                    if (i <= this.pageBlacklist.count)
                        this.pageBlacklist.numbers.push({ item: i, active: "" });
                }
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
                    this.model.ADR = {};
                }
            });
        },
        AddBlacklist: function () {
            $.post("/api/active/addBlacklist", { blacklist: this.model.blacklist }, (data) => {
                if (data != null) {
                    this.blacklist.push(data);
                    this.model.blacklist = "";
                }
            });
        },
        RemoveBlacklist: function (blacklist) {
            $.post("/api/active/deleteBlacklist", { blacklist }, (data) => {
                if (data) {
                    let temp = [];
                    for (let i = 0; i < this.blacklist.length; i++)
                    {
                        if (this.blacklist[i].column != blacklist) {
                            temp.push(this.blacklist[i]);
                        }
                    }
                    this.blacklist = temp;
                }
            });
        },
        changeClickedParam: function (param) {
            this.clickedParam = JSON.stringify(JSON.parse(param), undefined, 4);
        }
    }
});

admin.init();