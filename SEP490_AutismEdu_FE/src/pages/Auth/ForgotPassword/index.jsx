import ArrowBackIosNewIcon from '@mui/icons-material/ArrowBackIosNew';
import EscalatorWarningIcon from '@mui/icons-material/EscalatorWarning';
import LoadingButton from '@mui/lab/LoadingButton';
import { Box, FormControl, FormHelperText, InputLabel, OutlinedInput } from '@mui/material';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import { useSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import service from '~/plugins/services';
import checkValid from '~/utils/auth_form_verify';
import PAGES from '~/utils/pages';
function ForgotPassword() {
    const [emailError, setEmailError] = useState(null);
    const [submited, setSubmited] = useState(false);
    const [email, setEmail] = useState("");
    const [loading, setLoading] = useState(false);
    const { enqueueSnackbar } = useSnackbar();
    const [time, setTime] = useState(0);
    const [isRunning, setIsRunning] = useState(false);
    const INPUT_CSS = {
        width: "100%",
        borderRadius: "15px",
        ".MuiFormHelperText-root": {
            color: "red"
        }
    };
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
        if (emailError === null) {
            try {
                setLoading(true)
                await service.AuthenticationAPI.forgotPassword({
                    email: email
                }, (res) => {
                    setSubmited(true);
                    setTime(60);
                    setIsRunning(true);
                }, (err) => {
                    enqueueSnackbar(err.error[0], { variant: "error" });
                })
            } catch (error) {
                enqueueSnackbar("Lỗi hệ thống", { variant: "error" });
            } finally {
                setLoading(false)
            }
        }
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
                    <Typography variant='h5' sx={{ color: "text.secondary", mt: "20px" }}>Quên Mật Khẩu? 🔒</Typography>
                    <Typography sx={{ mt: "10px" }}>Nhập email của bạn và chúng tôi sẽ gửi cho bạn đường dẫn để đổi mật khẩu</Typography>
                    {
                        submited === false && (
                            <>
                                <Box mt="30px">
                                    <FormControl sx={{ ...INPUT_CSS }} variant="outlined">
                                        <InputLabel htmlFor="email">Email</InputLabel>
                                        <OutlinedInput id="email" label="Email" variant="outlined" type='email'
                                            onChange={(e) => {
                                                checkValid(e.target.value.trim(), 1, setEmailError);
                                                setEmail(e.target.value.trim())
                                            }}
                                            error={!!emailError}
                                            value={email}
                                        />
                                        {
                                            emailError && (
                                                <FormHelperText error id="accountId-error">
                                                    {emailError}
                                                </FormHelperText>
                                            )
                                        }
                                    </FormControl>
                                </Box>
                                <LoadingButton loading={loading} loadingIndicator="Đang chạy..." variant='contained'
                                    sx={{ width: "100%", marginTop: "20px" }}
                                    onClick={() => {
                                        const isValidEmail = checkValid(email, 1, setEmailError);
                                        if (isValidEmail) {
                                            handleSubmit();
                                        }
                                    }}>
                                    Gửi
                                </LoadingButton>
                            </>
                        )
                    }
                    {
                        submited === true && (
                            <>
                                <Typography mt={"12px"}>Link đặt lại mật khẩu đã được gửi tới <span style={{ color: "#3795BD" }}>{email}</span></Typography>
                                <LoadingButton loading={loading} loadingIndicator="Đang chạy..."
                                    onClick={() => {
                                        handleSubmit();
                                    }} disabled={time !== 0}>
                                    Gửi lại {time !== 0 && `(${time}s)`}
                                </LoadingButton>
                                <Button onClick={() => {
                                    setSubmited(false)
                                }} disabled={time !== 0}>Đổi email</Button>
                            </>
                        )
                    }
                    <Typography textAlign={'center'} mt="20px">
                        <Link to={PAGES.ROOT + PAGES.LOGIN_OPTION} style={{ color: "#666cff" }}>
                            <ArrowBackIosNewIcon sx={{ fontSize: "12px" }} /> Trở lại đăng nhập
                        </Link>
                    </Typography>
                </CardContent>
            </Card>
        </Box>
    );
}

export default ForgotPassword;
