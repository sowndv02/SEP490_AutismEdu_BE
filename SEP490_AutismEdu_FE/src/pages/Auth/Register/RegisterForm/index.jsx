import EscalatorWarningIcon from '@mui/icons-material/EscalatorWarning';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { LoadingButton } from '@mui/lab';
import { Box, Divider, FormControl, FormHelperText, IconButton, InputAdornment, InputLabel, MenuItem, OutlinedInput, Select } from '@mui/material';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import axios from 'axios';
import { useFormik } from 'formik';
import { enqueueSnackbar } from 'notistack';
import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import HtmlTooltip from '~/components/HtmlTooltip';
import services from '~/plugins/services';
import PAGES from '~/utils/pages';
import GoogleLogin from '../../Login/GoogleLogin';
function RegisterForm({ setVerify, setEmailVerify }) {
    const [showPassword, setShowPassword] = useState(false);
    const [loading, setLoading] = useState(false);
    const [provinces, setProvinces] = useState([]);
    const [districts, setDistricts] = useState([]);
    const [communes, setCommunes] = useState([]);
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
        handleGetProvince();
    }, [])

    const validate = values => {
        const errors = {};
        const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
        if (!values.email) {
            errors.email = "Bắt buộc"
        } else if (!emailRegex.test(values.email)) {
            errors.email = "Email của bạn không hợp lệ"
        }
        if (!values.fullName) {
            errors.fullName = 'Bắt buộc';
        }
        else if (!/^[a-zA-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂÊÔưăêôƠƯÀẢÃÁẠĂẮẰẲẴẶÂẦẤẨẪẬÈẺẼÉẸÊỀẾỂỄỆÌỈĨÍỊÒỎÕÓỌÔỒỐỔỖỘƠỜỚỞỠỢÙỦŨÚỤƯỪỨỬỮỰỲỶỸÝỴàảãáạăắằẳẵặâầấẩẫậèẻẽéẹêềếểễệìỉĩíịòỏõóọôồốổỗộơờớởỡợùủũúụưừứửữựỳỷỹýỵ\s]+$/.test(values.fullName)) {
            errors.fullName = "Tên không hợp lệ!"
        }
        else if (values.fullName.length > 50) {
            errors.fullName = 'Tên dưới 50 ký tự';
        }
        if (!values.phoneNumber) {
            errors.phoneNumber = 'Bắt buộc';
        } else if (!/^(0?)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$/.test(values.phoneNumber)) {
            errors.phoneNumber = 'Số điện thoại không hợp lệ';
        }
        if (!values.province) {
            errors.province = 'Bắt buộc';
        }
        if (!values.district) {
            errors.district = 'Bắt buộc';
        }
        if (!values.commune) {
            errors.commune = 'Bắt buộc';
        }
        if (!values.homeNumber) {
            errors.homeNumber = 'Bắt buộc';
        } else if (values.homeNumber.length > 100) {
            errors.homeNumber = 'Phải dưới 100 ký tự'
        }
        if (!values.password) {
            errors.password = 'Bắt buộc';
        }
        else if (values.password.length < 8 || values.password.length > 15) {
            errors.password = "Độ dài của mật khẩu từ 8 - 15 kí tự!"
        }
        else if (!/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@$?_-]).+$/.test(values.password)) {
            errors.password = 'Mật khẩu không hợp lệ'
        }
        if (!values.cfPassword) {
            errors.cfPassword = 'Bắt buộc';
        } else if (values.password !== values.cfPassword) {
            errors.cfPassword = 'Không giống mật khẩu';
        }
        return errors;
    };
    const formik = useFormik({
        initialValues: {
            email: '',
            fullName: '',
            province: '',
            district: '',
            commune: '',
            homeNumber: '',
            phoneNumber: '',
            password: '',
            cfPassword: ''
        },
        validate,
        onSubmit: async (values) => {
            const selectedCommune = communes.find(p => p.idCommune === values.commune);
            const selectedProvince = provinces.find(p => p.idProvince === values.province);
            const selectedDistrict = districts.find(p => p.idDistrict === values.district);
            const submitData = {
                email: values.email.trim(),
                fullName: values.fullName.trim(),
                password: values.password.trim(),
                address: `${selectedProvince.name}|${selectedDistrict.name}|${selectedCommune.name}|${values.homeNumber.trim()}`,
                phoneNumber: values.phoneNumber
            }
            setLoading(true);
            await services.AuthenticationAPI.register(submitData, (res) => {
                enqueueSnackbar("Đăng ký thành công!", { variant: "success" });
                setVerify(true);
                setEmailVerify(values.email);
            }, (err) => {
                enqueueSnackbar(err.error[0], { variant: "error" });
            })
            setLoading(false)
        }
    });
    const handleGetProvince = async () => {
        try {
            const data = await axios.get("https://vietnam-administrative-division-json-server-swart.vercel.app/province")
            setProvinces(data.data)
        } catch (error) {
            console.log(error);
        }
    }

    const handleGetDistrict = async (id) => {
        try {
            if (id?.length !== 0) {
                const data = await axios.get("https://vietnam-administrative-division-json-server-swart.vercel.app/district?idProvince=" + id);
                setDistricts(data.data);
                return data.data
            }
        } catch (error) {
            console.log(error);
        }
    }
    const handleGetCommunes = async (id) => {
        try {
            const data = await axios.get("https://vietnam-administrative-division-json-server-swart.vercel.app/commune?idDistrict=" + id);
            setCommunes(data.data);
            return data.data
        } catch (error) {
            console.log(error);
        }
    }
    return (
        <Box sx={{ bgcolor: "#f7f7f9", display: "flex", alignItems: "center", justifyContent: "center", py: "50px" }}>
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
                    <Typography variant='h5' sx={{ color: "text.secondary", mt: "20px" }}>Hãy Tạo Một Tài Khoản 🚀</Typography>
                    <Typography sx={{ mt: "10px" }}>Chúng tôi sẽ cung chấp cho bạn những dịch vụ mà chúng tôi có!</Typography>
                    <form onSubmit={formik.handleSubmit}>
                        <Box mt="30px">
                            <FormControl sx={{ ...INPUT_CSS, mt: "20px" }} variant="outlined">
                                <InputLabel htmlFor="fullname">Họ và tên</InputLabel>
                                <OutlinedInput id="fullname" label="Họ và tên" variant="outlined"
                                    name='fullName'
                                    value={formik.values.fullName}
                                    onChange={formik.handleChange}
                                    error={!!formik.errors.fullName}
                                />
                                {
                                    formik.errors.fullName && (
                                        <FormHelperText error>
                                            {formik.errors.fullName}
                                        </FormHelperText>
                                    )
                                }
                            </FormControl>
                            <FormControl sx={{ ...INPUT_CSS, mt: "20px" }} variant="outlined">
                                <InputLabel htmlFor="email">Email</InputLabel>
                                <OutlinedInput id="email" label="Email" variant="outlined" type='email'
                                    name='email'
                                    value={formik.values.email}
                                    onChange={formik.handleChange}
                                    error={!!formik.errors.email} />
                                {
                                    formik.errors.email && (
                                        <FormHelperText error>
                                            {formik.errors.email}
                                        </FormHelperText>
                                    )
                                }
                            </FormControl>
                            <FormControl sx={{ ...INPUT_CSS, mt: "20px" }} variant="outlined">
                                <InputLabel htmlFor="phoneNumber">Số điện thoại</InputLabel>
                                <OutlinedInput id="phoneNumber" label="Số điện thoại" variant="outlined" type='text'
                                    name='phoneNumber'
                                    value={formik.values.phoneNumber}
                                    onChange={formik.handleChange}
                                    error={!!formik.errors.phoneNumber} />
                                {
                                    formik.errors.phoneNumber && (
                                        <FormHelperText error>
                                            {formik.errors.phoneNumber}
                                        </FormHelperText>
                                    )
                                }
                            </FormControl>
                            <FormControl sx={{ ...INPUT_CSS, mt: "20px" }}>
                                <InputLabel id="province">Tỉnh / Thành phố</InputLabel>
                                <Select
                                    labelId="province"
                                    value={formik.values.province}
                                    label="Tỉnh / Thành phố"
                                    onChange={(event) => {
                                        const selectedProvince = event.target.value;
                                        if (selectedProvince && formik.values.province !== selectedProvince) {
                                            formik.handleChange(event);
                                            handleGetDistrict(event.target.value);
                                            setCommunes([]);
                                            formik.setFieldValue('district', '')
                                            formik.setFieldValue('commune', '')
                                        }
                                    }}
                                    error={!!formik.errors.province}
                                    name='province'
                                    renderValue={(selected) => {
                                        if (!selected || selected === "") {
                                            return <em>Tỉnh / TP</em>;
                                        }
                                        const selectedProvince = provinces.find(p => p.idProvince === selected);
                                        return selectedProvince ? selectedProvince.name : "";
                                    }}
                                >
                                    {
                                        provinces.length !== 0 && provinces?.map((province) => {
                                            return (
                                                <MenuItem value={province?.idProvince} key={province?.idProvince}>{province.name}</MenuItem>
                                            )
                                        })
                                    }
                                </Select>
                            </FormControl>
                            <FormControl sx={{ ...INPUT_CSS, mt: "20px" }}>
                                <InputLabel id="district">Huyện / Quận</InputLabel>
                                <Select
                                    labelId="district"
                                    value={formik.values.district}
                                    label="Huyện / Quận"
                                    name='district'
                                    onChange={(event) => {
                                        formik.handleChange(event); handleGetCommunes(event.target.value);
                                        formik.setFieldValue('commune', '')
                                    }}
                                    renderValue={(selected) => {
                                        if (!selected || selected === "") {
                                            return <em>Quận / Huyện</em>;
                                        }
                                        const selectedDistrict = districts.find(p => p.idDistrict === selected);
                                        return selectedDistrict ? selectedDistrict.name : <em>Quận / Huyện</em>;
                                    }}
                                    error={!!formik.errors.district}
                                    disabled={districts.length === 0}
                                >
                                    {
                                        districts.length !== 0 && districts?.map((district) => {
                                            return (
                                                <MenuItem value={district?.idDistrict} key={district?.idDistrict}>{district.name}</MenuItem>
                                            )
                                        })
                                    }
                                </Select>
                            </FormControl>
                            <FormControl sx={{ ...INPUT_CSS, mt: "20px" }}>
                                <InputLabel id="commune">Xã / Phường</InputLabel>
                                <Select
                                    labelId="commune"
                                    value={formik.values.commune}
                                    label="Xã / Phường"
                                    name='commune'
                                    onChange={formik.handleChange}
                                    renderValue={(selected) => {
                                        if (!selected || selected === "") {
                                            return <em>Xã / Phường</em>;
                                        }
                                        const selectedCommune = communes.find(p => p.idCommune === selected);
                                        return selectedCommune ? selectedCommune.name : <em>Xã / Phường</em>;
                                    }}
                                    error={!!formik.errors.commune}
                                    disabled={communes.length === 0}
                                >
                                    {
                                        communes.length !== 0 && communes?.map((commune) => {
                                            return (
                                                <MenuItem value={commune?.idCommune} key={commune?.idCommune}>{commune.name}</MenuItem>
                                            )
                                        })
                                    }
                                </Select>
                                {
                                    formik.errors.commune && (
                                        <FormHelperText error>
                                            {formik.errors.commune}
                                        </FormHelperText>
                                    )
                                }
                            </FormControl>
                            <FormControl sx={{ ...INPUT_CSS, mt: "20px" }}>
                                <InputLabel htmlFor="homeNumber">Số nhà</InputLabel>
                                <OutlinedInput id="homeNumber" label="Số nhà" variant="outlined" type='text'
                                    name='homeNumber'
                                    value={formik.values.homeNumber}
                                    onChange={formik.handleChange}
                                    error={!!formik.errors.homeNumber} />
                                {
                                    formik.errors.homeNumber && (
                                        <FormHelperText error>
                                            {formik.errors.homeNumber}
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
                                    value={formik.values.password}
                                    type={showPassword ? 'text' : 'password'}
                                    onChange={formik.handleChange}
                                    onKeyDown={(e) => {
                                        if (e.key === " ") {
                                            e.preventDefault();
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
                                    formik.errors.password && (
                                        <FormHelperText error id="password-error">
                                            <Box sx={{
                                                display: "flex",
                                                alignItems: "center",
                                                justifyContent: "space-between"
                                            }}>
                                                <p>{formik.errors.password}</p>
                                                <HtmlTooltip
                                                    title={
                                                        <React.Fragment>
                                                            <ul style={{ padding: "0", listStyle: "none" }}>
                                                                <li>Mật khẩu có từ 8 đến 15 ký tự</li>
                                                                <li>Chứa ít nhất một chữ số</li>
                                                                <li>Chứa ít nhất một chữ in hoa</li>
                                                                <li>Chứa ít nhất một trong những ký tự sau (! @ $ ? _ -)</li>
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
                                <InputLabel htmlFor="confirm-password">Nhập lại mật khẩu</InputLabel>
                                <OutlinedInput
                                    error={!!formik.errors.cfPassword}
                                    value={formik.values.cfPassword}
                                    id="confirm-password"
                                    name='cfPassword'
                                    type={showPassword ? 'text' : 'password'}
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
                                    label="Nhập lại mật khẩu"
                                />
                                {
                                    formik.errors.cfPassword && (
                                        <FormHelperText error>
                                            {formik.errors.cfPassword}
                                        </FormHelperText>
                                    )
                                }
                            </FormControl>
                        </Box>
                        <LoadingButton variant='contained' sx={{ width: "100%", marginTop: "20px" }}
                            loading={loading} loadingIndicator="Đang gửi..."
                            type='submit'>
                            Đăng ký
                        </LoadingButton>
                    </form>
                    <Typography sx={{ textAlign: "center", mt: "20px" }}>Bạn đã có tài khoản? <Link to={PAGES.ROOT + PAGES.LOGIN} style={{ color: "#666cff" }}>Đăng nhập</Link></Typography>
                    <Divider sx={{ mt: "15px" }}>hoặc</Divider>
                    <GoogleLogin />
                </CardContent>
            </Card>
        </Box>
    );
}

export default RegisterForm;
