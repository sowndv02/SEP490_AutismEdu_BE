import ArrowBackIosNewIcon from '@mui/icons-material/ArrowBackIosNew';
import EscalatorWarningIcon from '@mui/icons-material/EscalatorWarning';
import { LoadingButton } from '@mui/lab';
import { Box } from '@mui/material';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import service from '~/plugins/services';
function VerifyEmail({ email, setVerify, submitState }) {
    const [loading, setLoading] = useState(false);
    const [submited, setSubmited] = useState(submitState);
    const [time, setTime] = useState(0);
    const [isRunning, setIsRunning] = useState(false);
    useEffect(() => {
        if (loading) {
            handleSubmit();
        }
    }, [loading])

    useEffect(() => {
        let timer;
        if (isRunning && time > 0) {
            timer = setTimeout(() => {
                setTime((prevTime) => prevTime - 1);
            }, 1000);
        } else if (time === 0) {
            setIsRunning(false);
        }
        return () => clearTimeout(timer);
    }, [time, isRunning]);
    const handleSubmit = async () => {
        await service.AuthenticationAPI.verifyAccount({
            email
        }, (res) => {
            enqueueSnackbar("Kiểm tra emai của bạn!", { variant: "success" });
            setTime(60);
            setIsRunning(true);
        }, (err) => {
            enqueueSnackbar(err.error[0], { variant: "error" });
        })
        setLoading(false);
        setSubmited(true);
    }
    return (
        <Box sx={{ bgcolor: "#f7f7f9", height: "100vh", display: "flex", alignItems: "center", justifyContent: "center" }}>
            <Card sx={{
                width: "450px",
                boxShadow: "rgba(0, 0, 0, 0.35) 0px 5px 15px",
                borderRadius: "10px",
                p: "28px"
            }}>
                <CardContent>
                    <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", gap: 1 }}>
                        <EscalatorWarningIcon sx={{ color: "#394ef4", fontSize: "40px" }} />
                        <Typography sx={{ fontSize: 20, fontWeight: "bold", color: "text.secondary" }}>
                            AutismEdu
                        </Typography>
                    </Box>
                    <Typography variant='h5' sx={{ color: "text.secondary", mt: "20px" }}>Xác thực tài khoản! 👋</Typography>
                    <Typography sx={{ mt: "10px" }}>Làm ơn xác thực tài khoản của bạn để có thể đăng nhập vào hệ thống.</Typography>
                    <Box mt="30px">
                        {submited ? (
                            <Typography>Kiểm tra email ({email})</Typography>
                        ) : (
                            <Typography>Gửi link xác thực tới {email} </Typography>
                        )}
                    </Box>
                    <LoadingButton variant='contained' sx={{ width: "100%", marginTop: "20px" }} onClick={() => setLoading(true)}
                        loading={loading} loadingIndicator="Đang gửi..."
                        disabled={time !== 0}
                    >
                        {submited ? "Gửi lại" : "Gửi"} {time !== 0 && `(${time}s)`}
                    </LoadingButton>
                    <Typography textAlign={'center'} mt="20px" onClick={() => { setVerify(false) }} sx={{ cursor: "pointer" }}>
                        <ArrowBackIosNewIcon sx={{ fontSize: "12px" }} /> Trở lại đăng ký
                    </Typography>
                </CardContent>
            </Card>
        </Box>
    );
}

export default VerifyEmail;
