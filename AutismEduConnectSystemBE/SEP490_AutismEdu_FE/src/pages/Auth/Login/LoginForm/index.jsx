import EscalatorWarningIcon from '@mui/icons-material/EscalatorWarning';
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { LoadingButton } from '@mui/lab';
import { Box, Divider, FormControl, FormHelperText, IconButton, InputAdornment, InputLabel, OutlinedInput } from '@mui/material';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import Cookies from 'js-cookie';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useId, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import service from '~/plugins/services';
import checkValid from '~/utils/auth_form_verify';
import PAGES from '~/utils/pages';
import GoogleLogin from '../GoogleLogin';
import { useDispatch, useSelector } from "react-redux";
import { setUserInformation } from '~/redux/features/userSlice';
import { jwtDecode } from 'jwt-decode';
import services from '~/plugins/services';
function LoginForm({ setVerify, setEmailVerify, onLoginSuccess }) {
    const [showPassword, setShowPassword] = useState(false);
    const [emailError, setEmailError] = useState(null);
    const [passwordError, setPasswordError] = useState(null);
    const [email, setEmail] = useState("");
    const [loading, setLoading] = useState(false);
    const [password, setPassword] = useState("");
    const [userId, setUserId] = useState(null);
    const dispatch = useDispatch();
    const INPUT_CSS = {
        width: "100%",
        borderRadius: "15px",
        ".MuiFormHelperText-root": {
            color: "red"
        }
    };

    const handleMouseDownPassword = (event) => {
        event.preventDefault();
    };
    const handleClickShowPassword = () => setShowPassword((show) => !show);

    useEffect(() => {
        if (userId) {
            handleGetUserInformation();
        }
    }, [userId])

    const handleGetUserInformation = async() => {
        try {
            await services.UserManagementAPI.getUserById(userId, (res) => {
                dispatch(setUserInformation(res.result))
                enqueueSnackbar("Đăng nhập thành công!", { variant: "success" });
                onLoginSuccess();
            }, (error) => {
                enqueueSnackbar(error.error[0], { variant: "error" });
            })
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };
    const handleSubmit = async () => {
        setLoading(true);
        if (passwordError !== null || emailError !== null) {
            setLoading(false);
            return;
        } else {
            const checkEmail = checkValid(email, 1, setEmailError);
            const checkPw = checkValid(password, 2, setPasswordError);
            if (!checkEmail || !checkPw) {
                setLoading(false);
                return;
            } else {
                await service.AuthenticationAPI.login({
                    email,
                    password,
                    authenticationRole: "Parent"
                }, (res) => {
                    Cookies.set('access_token', res.result.accessToken, { expires: 30 })
                    Cookies.set('refresh_token', res.result.refreshToken, { expires: 365 })
                    const decodedToken = jwtDecode(res.result.accessToken);
                    setUserId(decodedToken['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'])
                }, (err) => {
                    console.log(err);
                    if (err.code !== 406) {
                        enqueueSnackbar(err.error[0], { variant: "error" });
                    }
                    else {
                        enqueueSnackbar(err.error[0], { variant: "warning" });
                        setVerify(true);
                        setEmailVerify(email);
                    }
                    setLoading(false);
                })
            }
        }
    }
    return (
        <Box sx={{ bgcolor: "#f7f7f9", height: "100vh", display: "flex", alignItems: "center", justifyContent: "center" }}>
            <Card sx={{
                width: "450px",
                height: "614px",
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
                    <Typography variant='h5' sx={{ color: "text.secondary", mt: "20px" }}>AutismEdu Xin Chào! 👋</Typography>
                    <Typography sx={{ mt: "10px" }}>Vui lòng đăng nhập vào tài khoản của bạn và khám phá dịch vụ của chúng tôi</Typography>
                    <Box mt="30px">
                        <FormControl sx={{ ...INPUT_CSS }} variant="outlined">
                            <InputLabel htmlFor="email">Email</InputLabel>
                            <OutlinedInput id="email" label="Email" variant="outlined" type='text'
                                value={email}
                                error={!!emailError}
                                onChange={(e) => {
                                    if (!e.target.value.includes(" ")) {
                                        checkValid(e.target.value, 1, setEmailError);
                                        setEmail(e.target.value.trim());
                                    }
                                }}
                            />
                            {
                                emailError && (
                                    <FormHelperText error id="accountId-error">
                                        {emailError}
                                    </FormHelperText>
                                )
                            }
                        </FormControl>
                        <FormControl sx={{ ...INPUT_CSS, mt: "20px" }} variant="outlined">
                            <InputLabel htmlFor="password">Mật khẩu</InputLabel>
                            <OutlinedInput
                                error={!!passwordError}
                                id="password"
                                type={showPassword ? 'text' : 'password'}
                                value={password}
                                onChange={(e) => {
                                    if (!e.target.value.includes(" ")) {
                                        checkValid(e.target.value, 2, setPasswordError);
                                        setPassword(e.target.value.trim());
                                    }
                                }}
                                endAdornment={
                                    <InputAdornment position="end">
                                        <IconButton
                                            aria-label="toggle password visibility"
                                            onClick={handleClickShowPassword}
                                            onMouseDown={handleMouseDownPassword}
                                            edge="end"
                                        >
                                            {showPassword ? <VisibilityOff /> : <Visibility />}
                                        </IconButton>
                                    </InputAdornment>
                                }
                                label="Mật khẩu"
                            />
                            {
                                passwordError && (
                                    <FormHelperText error id="accountId-error">
                                        {passwordError}
                                    </FormHelperText>
                                )
                            }
                        </FormControl>
                    </Box>
                    <Box sx={{ width: "100%", textAlign: "end", marginTop: "15px" }}>
                        <Link to={PAGES.ROOT + PAGES.FORGOTPASSWORD} style={{ color: "#666cff" }}>Quên mật khẩu?</Link>
                    </Box>
                    <LoadingButton variant='contained' sx={{ width: "100%", marginTop: "20px" }} onClick={handleSubmit}
                        loading={loading} loadingIndicator="Đang chạy..."
                    >
                        Đăng nhập
                    </LoadingButton>

                    <Typography sx={{ textAlign: "center", mt: "20px" }}>Bạn chưa có tài khoản? <Link to={PAGES.ROOT + PAGES.REGISTER} style={{ color: "#666cff" }}>Tạo tài khoản mới</Link></Typography>
                    <Divider sx={{ mt: "15px" }}>hoặc</Divider>
                    <GoogleLogin onLoginSuccess={onLoginSuccess} />
                </CardContent>
            </Card>
        </Box>
    );
}

export default LoginForm;