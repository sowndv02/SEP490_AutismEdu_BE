import GoogleIcon from '@mui/icons-material/Google';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { LoadingButton } from '@mui/lab';
import { Box, Divider, FormControl, FormHelperText, IconButton, InputAdornment, InputLabel, OutlinedInput, SvgIcon } from '@mui/material';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import { enqueueSnackbar } from 'notistack';
import React, { useEffect, useState } from 'react';
<<<<<<< HEAD
import { Link, useNavigate } from 'react-router-dom';
import TrelloIcon from '~/assets/trello.svg?react';
import HtmlTooltip from '~/components/HtmlTooltip';
import service from '~/plugins/services';
import PAGES from '~/utils/pages';
import checkValid from '~/utils/auth_form_verify';
=======
import { Link } from 'react-router-dom';
import TrelloIcon from '~/assets/trello.svg?react';
import HtmlTooltip from '~/components/HtmlTooltip';
import service from '~/plugins/services';
import checkValid from '~/utils/auth_form_verify';
import PAGES from '~/utils/pages';
import EscalatorWarningIcon from '@mui/icons-material/EscalatorWarning';
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
function RegisterForm({ setVerify, setEmailVerify }) {
    const [showPassword, setShowPassword] = useState(false);
    const [emailError, setEmailError] = useState(null);
    const [passwordConfirmError, setPasswordConfirmError] = useState(null);
    const [passwordError, setPasswordError] = useState(null);
<<<<<<< HEAD
=======
    const [fullNameError, setFullNameError] = useState(null);
    const [fullName, setFullName] = useState(null);
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    const [email, setEmail] = useState()
    const [password, setPassword] = useState("");
    const [cfPassword, setCfPassword] = useState("");
    const [loading, setLoading] = useState(false);
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
<<<<<<< HEAD

    const nav = useNavigate();
    const handleClickShowPassword = () => setShowPassword((show) => !show);

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
            }
            else {
                if (passwordConfirmError === "Confirm password doesn't match with the password" && value === cfPassword) {
                    setPasswordConfirmError(null);
                }
                setPasswordError(null);
                return true;
            }
        }
        if (field === 3) {
            if (value === "") {
                setPasswordConfirmError("Please enter confirm password");
                return false;
            } else if (value !== password) {
                setPasswordConfirmError("Confirm password doesn't match with the password");
                return false;
            } else {
                setPasswordConfirmError(null);
                return true;
            }
        }
    }
=======
    const handleClickShowPassword = () => setShowPassword((show) => !show);

>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    useEffect(() => {
        if (loading) {
            handleSubmit();
        }
    }, [loading])
    const handleSubmit = async () => {
        if (emailError !== null || passwordError !== null || passwordConfirmError !== null) {
            setLoading(false);
            return;
        } else {
            const checkPw = checkValid(password, 2, setPasswordError);
            const checkCfPw = checkValid(cfPassword, 3, setPasswordConfirmError, password);
            const checkEmail = checkValid(email, 1, setEmailError);
            if (!checkPw || !checkCfPw || !checkEmail) {
                setLoading(false);
                return;
            } else {
                await service.AuthenticationAPI.register({
                    email,
                    password,
                    role: "user"
                }, (res) => {
<<<<<<< HEAD
                    enqueueSnackbar("Register Successfully!", { variant: "success" });
=======
                    enqueueSnackbar("Đăng ký thành công!", { variant: "success" });
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                    setVerify(true);
                    setEmailVerify(email);
                }, (err) => {
                    if (err.code === 500) {
<<<<<<< HEAD
                        enqueueSnackbar("Failed to register!", { variant: "error" });
=======
                        enqueueSnackbar("Đăng ký thật bại!", { variant: "error" });
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                    }
                    else enqueueSnackbar(err.error[0], { variant: "error" });
                })
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
<<<<<<< HEAD
                        <SvgIcon component={TrelloIcon} inheritViewBox sx={{ color: 'blue' }} />
                        <Typography sx={{ fontSize: 20, fontWeight: "bold", color: "text.secondary" }}>
                            My App
                        </Typography>
                    </Box>
                    <Typography variant='h5' sx={{ color: "text.secondary", mt: "20px" }}>Adventure starts here 🚀</Typography>
                    <Typography sx={{ mt: "10px" }}>Make your app management easy and fun!</Typography>
=======
                        <EscalatorWarningIcon sx={{ color: "#394ef4", fontSize: "40px" }} />
                        <Typography sx={{ fontSize: 20, fontWeight: "bold", color: "text.secondary" }}>
                            AutismEdu
                        </Typography>
                    </Box>
                    <Typography variant='h5' sx={{ color: "text.secondary", mt: "20px" }}>Hãy Tạo Một Tài Khoản 🚀</Typography>
                    <Typography sx={{ mt: "10px" }}>Chúng tôi sẽ cung chấp cho bạn những dịch vụ mà chúng tôi có!</Typography>
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                    <Box mt="30px">
                        <FormControl sx={{ ...INPUT_CSS, mt: "20px" }} variant="outlined">
                            <InputLabel htmlFor="email">Email</InputLabel>
                            <OutlinedInput id="email" label="Email" variant="outlined" type='email'
                                onChange={(e) => { checkValid(e.target.value, 1, setEmailError); setEmail(e.target.value) }}
                                error={!!emailError} />
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
                                onChange={(e) => { checkValid(e.target.value, 2, setPasswordError); setPassword(e.target.value) }}
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
                                    <FormHelperText error id="password-error">
                                        <Box sx={{
                                            display: "flex",
                                            alignItems: "center",
                                            justifyContent: "space-between"
                                        }}>
                                            <p>{passwordError}</p>
                                            <HtmlTooltip
                                                title={
                                                    <React.Fragment>
                                                        <ul style={{ padding: "0", listStyle: "none" }}>
                                                            <li>Password length from 8 to 15 characters</li>
                                                            <li>Contains at least 1 number</li>
                                                            <li>Contains lowercase and uppercase letters</li>
                                                            <li>Contains at least one of the following special characters (. ! & %)</li>
                                                        </ul>
                                                    </React.Fragment>
                                                }
                                            >
                                                <HelpOutlineIcon sx={{ fontSize: "16px" }} />
                                            </HtmlTooltip>
                                        </Box>
                                    </FormHelperText>
                                )
                            }
                        </FormControl>
                        <FormControl sx={{ ...INPUT_CSS, mt: "20px" }} variant="outlined">
<<<<<<< HEAD
                            <InputLabel htmlFor="confirm-password">Confirm Password</InputLabel>
=======
                            <InputLabel htmlFor="confirm-password">Nhập lại mật khẩu</InputLabel>
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                            <OutlinedInput
                                error={!!passwordConfirmError}
                                value={cfPassword}
                                id="confirm-password"
                                type={showPassword ? 'text' : 'password'}
                                onChange={(e) => {
                                    if (!e.target.value.includes(" ")) {
                                        setCfPassword(e.target.value);
                                        checkValid(e.target.value, 3, setPasswordConfirmError, password);
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
                                label="Nhập lại mật khẩu"
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                            />
                            {
                                passwordConfirmError && (
                                    <FormHelperText error id="cf-password-id">
                                        {passwordConfirmError}
                                    </FormHelperText>
                                )
                            }
                        </FormControl>
                    </Box>
                    <LoadingButton variant='contained' sx={{ width: "100%", marginTop: "20px" }}
                        loading={loading} loadingIndicator="Sending..."
                        onClick={() => {
                            setLoading(true);
                        }}>
<<<<<<< HEAD
                        Sign Up
                    </LoadingButton>

                    <Typography sx={{ textAlign: "center", mt: "20px" }}>Already have an account? <Link to={PAGES.LOGIN} style={{ color: "#666cff" }}>Sign in instead</Link></Typography>
                    <Divider sx={{ mt: "15px" }}>or</Divider>
=======
                        Đăng ký
                    </LoadingButton>

                    <Typography sx={{ textAlign: "center", mt: "20px" }}>Bạn đã có tài khoản? <Link to={PAGES.ROOT + PAGES.LOGIN} style={{ color: "#666cff" }}>Đăng nhập</Link></Typography>
                    <Divider sx={{ mt: "15px" }}>hoặc</Divider>
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                    <Box sx={{ display: "flex", justifyContent: "center" }}>
                        <IconButton>
                            <GoogleIcon sx={{ color: "#dd4b39 " }} />
                        </IconButton>
                    </Box>
                </CardContent>
            </Card>
        </Box>
    );
}

export default RegisterForm;
