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
import { Link, useNavigate } from 'react-router-dom';
import TrelloIcon from '~/assets/trello.svg?react';
import HtmlTooltip from '~/components/HtmlTooltip';
import service from '~/plugins/services';
import PAGES from '~/utils/pages';
import checkValid from '~/utils/auth_form_verify';
function RegisterForm({ setVerify, setEmailVerify }) {
    const [showPassword, setShowPassword] = useState(false);
    const [emailError, setEmailError] = useState(null);
    const [passwordConfirmError, setPasswordConfirmError] = useState(null);
    const [passwordError, setPasswordError] = useState(null);
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
    const handleClickShowPassword = () => setShowPassword((show) => !show);

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
                    enqueueSnackbar("Register Successfully!", { variant: "success" });
                    setVerify(true);
                    setEmailVerify(email);
                }, (err) => {
                    if (err.code === 500) {
                        enqueueSnackbar("Failed to register!", { variant: "error" });
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
                        <SvgIcon component={TrelloIcon} inheritViewBox sx={{ color: 'blue' }} />
                        <Typography sx={{ fontSize: 20, fontWeight: "bold", color: "text.secondary" }}>
                            My App
                        </Typography>
                    </Box>
                    <Typography variant='h5' sx={{ color: "text.secondary", mt: "20px" }}>Adventure starts here 🚀</Typography>
                    <Typography sx={{ mt: "10px" }}>Make your app management easy and fun!</Typography>
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
                            <InputLabel htmlFor="password">Password</InputLabel>
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
                                label="Password"
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
                            <InputLabel htmlFor="confirm-password">Confirm Password</InputLabel>
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
                                label="Confirm Password"
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
                        Sign Up
                    </LoadingButton>

                    <Typography sx={{ textAlign: "center", mt: "20px" }}>Already have an account? <Link to={PAGES.LOGIN} style={{ color: "#666cff" }}>Sign in instead</Link></Typography>
                    <Divider sx={{ mt: "15px" }}>or</Divider>
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
