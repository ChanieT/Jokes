$(() => {
    setInterval(() => {
        const like=checkLike(false);
        $.get('/home/stillLike', like, function (result) {
            if (!result.liked) {
                $("#like-btn").attr('disabled', true);
                $("#dislike-btn").attr('disabled', true);
            }
        });
    }, 100000);

    $("#like-btn").on('click', function () {
        const userId = $("#userId").val();
        const jokeId = $("#jokeId").val();
        $.post('/home/like', { userId, jokeId }, function () {

        });
        $("#like-btn").attr('disabled', true);
        $('#dislike-btn').attr('disabled', false);
    });

    $("#dislike-btn").on('click', function () {
        const userId = $("#userId").val();
        const jokeId = $("#jokeId").val();
        $.post('/home/dislike', { userId, jokeId }, function () {

        });
        $("#dislike-btn").attr('disabled', true);
        $('#like-btn').attr('disabled', false);
    });

    function checkLike(liked) {
        const like = {
            userId: $("#userId").val(),
            jokeId: $("#jokeId").val(),
            liked:liked
        }
        return like;
    }

});