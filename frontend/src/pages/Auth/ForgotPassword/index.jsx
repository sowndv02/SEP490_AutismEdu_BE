import ArrowBackIosNewIcon from '@mui/icons-material/ArrowBackIosNew';
import { Box, FormControl, FormHelperText, InputLabel, OutlinedInput, Snackbar, SvgIcon } from '@mui/material';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import TrelloIcon from '~/assets/trello.svg?react';
import PAGES from '~/utils/pages';
import service from '~/plugins/services'
import LoadingButton from '@mui/lab/LoadingButton';
import { useSnackbar } from 'notistack';
import checkValid from '~/utils/auth_form_verify';
import EscalatorWarningIcon from '@mui/icons-material/EscalatorWarning';
function ForgotPassword() {
    const [emailError, setEmailError] = useState(null);
    const [submited, setSubmited] = useState(false);
    const [email, setEmail] = useState("");
    const [loading, setLoading] = useState(false);
    const { enqueueSnackbar } = useSnackbar();

    const INPUT_CSS = {
        width: "100%",
        borderRadius: "15px",
        ".MuiFormHelperText-root": {
            color: "red"
        }
    };
    useEffect(() => {
        if (loading) {
            handleSubmit();
        }
    }, [loading])

    const handleSubmit = async () => {
        if (emailError === null) {
            await service.AuthenticationAPI.forgotPassword({
                email: email
            }, (res) => {
                setSubmited(true);
            }, (err) => {
                console.log(err);
                enqueueSnackbar("Email không hợp lệ", { variant: "error" });
                setLoading(false)
            })
            setLoading(false)
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
                                                checkValid(e.target.value, 1, setEmailError);
                                                setEmail(e.target.value)
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
                                <LoadingButton loading={loading} loadingIndicator="Sending..." variant='contained'
                                    sx={{ width: "100%", marginTop: "20px" }}
                                    onClick={() => {
                                        const isValidEmail = checkValid(email, 1, setEmailError);
                                        if (isValidEmail) {
                                            setLoading(true);
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
                                <Typography mt={"12px"}>The reset link has been sent to email <span style={{ color: "#3795BD" }}>{email}</span></Typography>
                                <LoadingButton loading={loading} loadingIndicator="Sending..."
                                    onClick={() => {
                                        setLoading(true);
                                    }}>
                                    Gửi lại
                                </LoadingButton>
                                <Button onClick={() => {
                                    setSubmited(false)
                                }}>Đổi email</Button>
                            </>
                        )
                    }
                    <Typography textAlign={'center'} mt="20px">
                        <Link to={PAGES.LOGIN} style={{ color: "#666cff" }}>
                            <ArrowBackIosNewIcon sx={{ fontSize: "12px" }} /> Trở lại đăng nhập
                        </Link>
                    </Typography>
                </CardContent>
            </Card>
        </Box>
    );
}

export default ForgotPassword;
