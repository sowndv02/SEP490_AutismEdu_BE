<<<<<<< HEAD
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { Box, Divider, FormControl, FormHelperText, IconButton, InputAdornment, InputLabel, OutlinedInput, SvgIcon, TextField } from '@mui/material';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import { useEffect, useState } from 'react';
import TrelloIcon from '~/assets/trello.svg?react';
import Cookies from 'js-cookie';
import { Link, useNavigate } from 'react-router-dom';
import PAGES from '~/utils/pages';
import service from '~/plugins/services'
import { LoadingButton } from '@mui/lab';
import { enqueueSnackbar } from 'notistack';
import GoogleLogin from '../GoogleLogin';
import checkValid from '~/utils/auth_form_verify';
=======
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
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
function LoginForm({ setVerify, setEmailVerify }) {
    const [showPassword, setShowPassword] = useState(false);
    const [emailError, setEmailError] = useState(null);
    const [passwordError, setPasswordError] = useState(null);
    const [email, setEmail] = useState("");
    const [loading, setLoading] = useState(false);
    const [password, setPassword] = useState("");
<<<<<<< HEAD
    const nav = useNavigate();
=======
    const [userId, setUserId] = useState(null);
    const nav = useNavigate();
    const dispatch = useDispatch();
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
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
        if (loading) {
            handleSubmit();
        }
    }, [loading])
<<<<<<< HEAD
    const checkValid = (value, field) => {
        const rgPassword = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[.!&%@]).+$/
        const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
        if (field === 1) {
            if (value === "") {
                setEmailError("Please enter email");
                return false;
            } else if (!emailRegex.test(value)) {
                setEmailError("Email is not valid");
                return false;
            } else {
                setEmailError(null);
                return true;
            }
        }
        if (field === 2) {
            if (value === "") {
                setPasswordError("Please enter password");
                return false;
            } else if (value.length < 8) {
                setPasswordError("Password must be more than 8 characters");
                return false;
            } else if (value.length > 15) {
                setPasswordError("Password must be less than 15 characters");
                return false;
            } else if (!rgPassword.test(value)) {
                setPasswordError("Password is invalid!");
                return false;
            } else {
                setPasswordError(null);
                return true;
            }
        }
    }
=======
    console.log(userId);

    useEffect(() => {
        if (userId) {
            console.log("zoday");
            services.UserManagementAPI.getUserById(userId, (res) => {
                dispatch(setUserInformation(res.result))
                enqueueSnackbar("Đăng nhập thành công!", { variant: "success" });
                nav(`${PAGES.ROOT}`)
            }, (error) => {
                enqueueSnackbar("Đăng nhập thất bại!", { variant: "error" });
                console.log(error);
            })
            setLoading(false)
        }
    }, [userId])
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    const handleSubmit = async () => {
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
                    password
                }, (res) => {
<<<<<<< HEAD
                    console.log(res);
                    Cookies.set('access_token', res.result.accessToken, { expires: 30 })
                    Cookies.set('refresh_token', res.result.refreshToken, { expires: 365 })
                    enqueueSnackbar("Login successfully!", { variant: "success" });
                }, (err) => {
                    console.log(err);
                    if (err.code === 500) {
                        enqueueSnackbar("Failed to reset password!", { variant: "error" });
                    } else if (err.code === 406) {
                        enqueueSnackbar(err.error[0], { variant: "error" });
                        setVerify(true);
                        setEmailVerify(email);
                    }
                    else enqueueSnackbar(err.error[0], { variant: "error" });
                })
                setLoading(false)
=======
                    Cookies.set('access_token', res.result.accessToken, { expires: 30 })
                    Cookies.set('refresh_token', res.result.refreshToken, { expires: 365 })
                    const decodedToken = jwtDecode(res.result.accessToken);
                    setUserId(decodedToken['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'])
                }, (err) => {
                    if (err.code === 500) {
                        enqueueSnackbar("Đăng nhập thất bại!", { variant: "error" });
                    } else if (err.code === 406) {
                        enqueueSnackbar("Tài khoản này chưa được kích hoạt!", { variant: "warning" });
                        setVerify(true);
                        setEmailVerify(email);
                    }
                    else enqueueSnackbar("Tài khoản hoặc mật khẩu không đúng!", { variant: "error" });
                    setLoading(false)
                })
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
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
<<<<<<< HEAD
                        <SvgIcon component={TrelloIcon} inheritViewBox sx={{ color: 'blue' }} />
                        <Typography sx={{ fontSize: 20, fontWeight: "bold", color: "text.secondary" }}>
                            My App
                        </Typography>
                    </Box>
                    <Typography variant='h5' sx={{ color: "text.secondary", mt: "20px" }}>Welcome to MyApp! 👋</Typography>
                    <Typography sx={{ mt: "10px" }}>Please sign-in to your account and start the adventure</Typography>
                    <Box mt="30px">
                        <FormControl sx={{ ...INPUT_CSS }} variant="outlined">
                            <InputLabel htmlFor="email">Email or Username</InputLabel>
                            <OutlinedInput id="email" label="Email or username" variant="outlined" type='text'
=======
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
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                                value={email}
                                error={!!emailError}
                                onChange={(e) => {
                                    if (!e.target.value.includes(" ")) {
<<<<<<< HEAD
                                        console.log("zoday");
=======
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                                        checkValid(e.target.value, 1, setEmailError);
                                        setEmail(e.target.value);
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
<<<<<<< HEAD
                            <InputLabel htmlFor="password">Password</InputLabel>
=======
                            <InputLabel htmlFor="password">Mật khẩu</InputLabel>
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                            <OutlinedInput
                                error={!!passwordError}
                                id="password"
                                type={showPassword ? 'text' : 'password'}
                                value={password}
                                onChange={(e) => {
                                    if (!e.target.value.includes(" ")) {
                                        checkValid(e.target.value, 2, setPasswordError);
                                        setPassword(e.target.value);
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
<<<<<<< HEAD
                                label="Password"
=======
                                label="Mật khẩu"
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
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
<<<<<<< HEAD
                        <Link to={PAGES.FORGOTPASSWORD} style={{ color: "#666cff" }}>Forgot Password?</Link>
=======
                        <Link to={PAGES.ROOT + PAGES.FORGOTPASSWORD} style={{ color: "#666cff" }}>Quên mật khẩu?</Link>
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                    </Box>
                    <LoadingButton variant='contained' sx={{ width: "100%", marginTop: "20px" }} onClick={() => setLoading(true)}
                        loading={loading} loadingIndicator="Sending..."
                    >
<<<<<<< HEAD
                        Sign In
                    </LoadingButton>

                    <Typography sx={{ textAlign: "center", mt: "20px" }}>New on our platform? <Link to={PAGES.REGISTER} style={{ color: "#666cff" }}>Create an account</Link></Typography>
                    <Divider sx={{ mt: "15px" }}>or</Divider>
=======
                        Đăng nhập
                    </LoadingButton>

                    <Typography sx={{ textAlign: "center", mt: "20px" }}>Bạn chưa có tài khoản? <Link to={PAGES.ROOT + PAGES.REGISTER} style={{ color: "#666cff" }}>Tạo tài khoản mới</Link></Typography>
                    <Divider sx={{ mt: "15px" }}>hoặc</Divider>
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                    <GoogleLogin />
                </CardContent>
            </Card>
        </Box>
    );
}

export default LoginForm;
