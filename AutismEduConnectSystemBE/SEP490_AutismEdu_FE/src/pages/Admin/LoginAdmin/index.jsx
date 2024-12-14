import EscalatorWarningIcon from '@mui/icons-material/EscalatorWarning';
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { LoadingButton } from '@mui/lab';
import { Box, FormControl, FormHelperText, IconButton, InputAdornment, InputLabel, OutlinedInput } from '@mui/material';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import { useFormik } from 'formik';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import { useDispatch } from "react-redux";
import { useNavigate } from 'react-router-dom';
import { default as service, default as services } from '~/plugins/services';
import { setAdminInformation } from '~/redux/features/adminSlice';
import PAGES from '~/utils/pages';
function LoginAdmin() {
    const [showPassword, setShowPassword] = useState(false);
    const [loading, setLoading] = useState(false);
    const [userId, setUserId] = useState(null);
    const [role, setRole] = useState(null);
    const dispatch = useDispatch();
    const nav = useNavigate();
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
        if (userId && role) {
            services.UserManagementAPI.getUserById(userId, (res) => {
                enqueueSnackbar("Đăng nhập thành công!", { variant: "success" });
                dispatch(setAdminInformation(res.result));
                if (role === "Manager") {
                    nav(PAGES.DASHBOARD)
                } else if (role === "Admin") {
                    nav(PAGES.USERMANAGEMENT)
                } else {
                    nav(PAGES.PARENT_TUTOR_MAMAGEMENT)
                }
            }, (error) => {
                enqueueSnackbar(error.error[0], { variant: "error" });
                console.log(error);
            })
            setLoading(false)
        }
    }, [userId, role])

    const validate = (values) => {
        const errors = {}
        if (!values.email) {
            errors.email = "Bắt buộc"
        }
        if (!values.password) {
            errors.password = "Bắt buộc"
        }
        return errors
    }
    const formik = useFormik({
        initialValues: {
            email: "",
            password: ""
        }, validate,
        onSubmit: (values) => {
            handleSubmit();
        }
    })
    const handleSubmit = async () => {
        setLoading(true);
        await service.AuthenticationAPI.login({
            email: formik.values.email,
            password: formik.values.password,
            authenticationRole: "Admin"
        }, (res) => {
            Cookies.set('access_token', res.result.accessToken, { expires: 30 })
            Cookies.set('refresh_token', res.result.refreshToken, { expires: 365 })
            const decodedToken = jwtDecode(res.result.accessToken);
            setUserId(decodedToken['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'])
            setRole(decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'])
        }, (err) => {
            enqueueSnackbar(err.error[0], { variant: "error" });
        })
        setLoading(false)
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
                    <form onSubmit={formik.handleSubmit}>
                        <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", gap: 1 }}>
                            <EscalatorWarningIcon sx={{ color: "#394ef4", fontSize: "40px" }} />
                            <Typography sx={{ fontSize: 20, fontWeight: "bold", color: "text.secondary" }}>
                                AutismEdu
                            </Typography>
                        </Box>
                        <Typography variant='h4' textAlign="center" mt={3}>Đăng nhập</Typography>
                        <Box mt="30px">
                            <FormControl sx={{ ...INPUT_CSS }} variant="outlined">
                                <InputLabel htmlFor="email">Email</InputLabel>
                                <OutlinedInput id="email" label="Email" variant="outlined" type='text'
                                    value={formik.values.email}
                                    name='email'
                                    error={!!formik.errors.email}
                                    onChange={formik.handleChange}
                                />
                                {
                                    formik.errors.email && (
                                        <FormHelperText error>
                                            {formik.errors.email}
                                        </FormHelperText>
                                    )
                                }
                            </FormControl>
                            <FormControl sx={{ ...INPUT_CSS, mt: "20px" }} variant="outlined">
                                <InputLabel htmlFor="password">Mật khẩu</InputLabel>
                                <OutlinedInput
                                    error={!!formik.errors.password}
                                    id="password"
                                    name='password'
                                    type={showPassword ? 'text' : 'password'}
                                    value={formik.values.password}
                                    onChange={formik.handleChange}
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
                                    formik.errors.password && (
                                        <FormHelperText error>
                                            {formik.errors.password}
                                        </FormHelperText>
                                    )
                                }
                            </FormControl>
                        </Box>
                        <LoadingButton variant='contained' sx={{ width: "100%", marginTop: "20px" }} type='submit'
                            loading={loading} loadingIndicator="Đang chạy..."
                        >
                            Đăng nhập
                        </LoadingButton>
                    </form>
                </CardContent>
            </Card>
        </Box >
    );
}

export default LoginAdmin;
