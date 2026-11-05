window.encurtador = {
    copyText: async function (text) {
        try {
            if (!navigator.clipboard || !navigator.clipboard.writeText) {
                return false;
            }
            await navigator.clipboard.writeText(text);
            return true;
        } catch (e) {
            return false;
        }
    },
    // Deslocamento em minutos a leste de UTC (sinal oposto ao de Date.getTimezoneOffset).
    getOffsetMinutesForLocal: function (year, month, day, hour, minute) {
        return -new Date(year, month - 1, day, hour, minute).getTimezoneOffset();
    },
    getOffsetMinutesForInstant: function (unixMilliseconds) {
        return -new Date(unixMilliseconds).getTimezoneOffset();
    }
};
