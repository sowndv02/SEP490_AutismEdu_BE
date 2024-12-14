import { Password } from '@mui/icons-material';
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { Button, Checkbox, Divider, FormControl, FormHelperText, IconButton, InputAdornment, ListItemText, MenuItem, OutlinedInput, Select } from '@mui/material';
import Box from '@mui/material/Box';
import Modal from '@mui/material/Modal';
import Typography from '@mui/material/Typography';
import { useFormik } from 'formik';
import { enqueueSnackbar } from 'notistack';
import * as React from 'react';
import { useEffect } from 'react';
import { useState } from 'react';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 800,
    bgcolor: 'background.paper',
    boxShadow: 24,
    p: 4,
    borderRadius: "10px"
};
function UserCreation({ setUsers, currentPage }) {
    const [open, setOpen] = React.useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const [showPassword, setShowPassword] = useState(false);
    const [loading, setLoading] = useState(false);
    const [roles, setRoles] = useState([]);
    const validate = values => {
        const errors = {};
        if (!values.fullName) {
            errors.fullName = 'Bắt buộc';
        } else if (values.fullName.length > 20) {
            errors.fullName = 'Độ dài nhỏ hơn 20 ký tự';
        }
        if (!values.email) {
            errors.email = 'Bắt buộc'
        } else if (!/^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/.test(values.email)) {
            errors.email = 'Email không hợp lệ';
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
        if (!values.role) {
            errors.role = "Bắt buộc";
        }
        return errors;
    };

    useEffect(() => {
        if (open) {
            getRoles();
        }
        if (!open) {
            formik.resetForm();
        }
    }, [open]);
    const handleMouseDownPassword = (event) => {
        event.preventDefault();
    };

    const getRoles = async () => {
        try {
            setLoading(true);
            await services.RoleManagementAPI.getRoles((res) => {
                const returnRole = res.result.filter((r) => {
                    return r.name === "Manager" || r.name === 'Staff'
                })
                setRoles(returnRole);
            },
                (err) => {
                    console.log(err);
                })
            setLoading(false);
        } catch (error) {
            console.log(error);
            setLoading(false);
        }
    }
    const handleClickShowPassword = () => setShowPassword((show) => !show);
    const formik = useFormik({
        initialValues: {
            fullName: '',
            email: '',
            phoneNumber: '',
            password: '',
            cfPassword: '',
            role: ''
        },
        validate,
        onSubmit: async (values) => {
            try {
                setLoading(true);
                setOpen(false);
                await services.UserManagementAPI.createUser(
                    {
                        FullName: values.fullName,
                        Email: values.email,
                        PhoneNumber: values.phoneNumber,
                        Password: values.password,
                        ConfirmPassword: values.cfPassword,
                        RoleId: values.role,
                        IsLockedOut: false
                    }
                    , (res) => {
                        if (currentPage === 1) {
                            setUsers(pre => [res.result, ...pre])
                        }
                        enqueueSnackbar("Tạo tài khoản thành công!", { variant: "success" });
                    }, (error) => {
                        enqueueSnackbar(error.error[0], { variant: "error" });
                    })
                setLoading(false);
            } catch (error) {
                enqueueSnackbar("Tạo tài khoản thất bại!", { variant: "error" });
            }
        }
    });
    return (
        <div>
            <Button variant="contained" onClick={handleOpen}>Tạo tài khoản mới</Button>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style}>
                    <Typography id="modal-modal-title" variant="h6" sx={{ color: "black", fontWeight: "bold" }}>
                        Tạo tài khoản mới
                    </Typography>
                    <Divider />
                    <form onSubmit={formik.handleSubmit}>
                        <Box sx={{ display: "flex", gap: "10px", mt: "20px" }}>
                            <FormControl sx={{ width: '50%' }} variant="outlined">
                                <label style={{ fontWeight: "bold", color: "black", fontSize: "14px" }} htmlFor="fullName">Họ và tên</label>
                                <OutlinedInput
                                    error={formik.errors.fullName}
                                    name='fullName'
                                    id="fullName"
                                    onChange={formik.handleChange}
                                    value={formik.values.fullName}
                                    size='small'
                                />
                                {
                                    formik.errors.fullName && (
                                        <FormHelperText error>
                                            {formik.errors.fullName}
                                        </FormHelperText>
                                    )
                                }
                            </FormControl>
                            <FormControl sx={{ width: '50%' }} variant="outlined">
                                <label style={{ fontWeight: "bold", color: "black", fontSize: "14px" }} htmlFor="email">Email</label>
                                <OutlinedInput
                                    error={formik.errors.email}
                                    name='email'
                                    id="email"
                                    onChange={formik.handleChange}
                                    value={formik.values.email}
                                    size='small'
                                />
                                {
                                    formik.errors.email && (
                                        <FormHelperText error>
                                            {formik.errors.email}
                                        </FormHelperText>
                                    )
                                }
                            </FormControl>
                        </Box>
                        <FormControl sx={{ width: '100%', marginTop: "10px" }} variant="outlined">
                            <label style={{ fontWeight: "bold", color: "black", fontSize: "14px" }} htmlFor="phone">Số điện thoại</label>
                            <OutlinedInput
                                error={formik.errors.phoneNumber}
                                name='phoneNumber'
                                id="phone"
                                onChange={formik.handleChange}
                                value={formik.values.phoneNumber}
                                size='small'
                            />
                            {
                                formik.errors.phoneNumber && (
                                    <FormHelperText error>
                                        {formik.errors.phoneNumber}
                                    </FormHelperText>
                                )
                            }
                        </FormControl>
                        <FormControl sx={{ width: '100%', marginTop: "10px" }} variant="outlined">
                            <label style={{ fontWeight: "bold", color: "black", fontSize: "14px" }} htmlFor="password">Mật khẩu</label>
                            <OutlinedInput
                                error={formik.errors.password}
                                name='password'
                                id="password"
                                onChange={formik.handleChange}
                                value={formik.values.password}
                                size='small'
                                type={showPassword ? 'text' : 'password'}
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
                            />
                            {
                                formik.errors.password && (
                                    <FormHelperText error>
                                        {formik.errors.password}
                                    </FormHelperText>
                                )
                            }
                        </FormControl>
                        <FormControl sx={{ width: '100%', marginTop: "10px" }} variant="outlined">
                            <label style={{ fontWeight: "bold", color: "black", fontSize: "14px" }} htmlFor="cfPassword">Nhập lại mật khẩu</label>
                            <OutlinedInput
                                error={formik.errors.password}
                                name='cfPassword'
                                id="cfPassword"
                                onChange={formik.handleChange}
                                value={formik.values.cfPassword}
                                size='small'
                                type={showPassword ? 'text' : 'password'}
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
                            />
                            {
                                formik.errors.cfPassword && (
                                    <FormHelperText error>
                                        {formik.errors.cfPassword}
                                    </FormHelperText>
                                )
                            }
                        </FormControl>
                        <Box sx={{ display: "flex", gap: "10px", mt: "20px" }}>
                            <FormControl sx={{ width: "50%" }} size='small'>
                                <label style={{ fontWeight: "bold", color: "black", fontSize: "14px" }} htmlFor="role">Vai trò</label>
                                <Select
                                    labelId="roles"
                                    value={formik.values.role}
                                    name='role'
                                    onChange={formik.handleChange}
                                >
                                    {roles?.map((role) => (
                                        <MenuItem key={role.id} value={role.id}>{role.name}</MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                        </Box>
                        <Box sx={{ display: "flex", gap: "10px", mt: "20px" }}>
                            <Button variant='outlined' color='inherit' sx={{ width: "50%" }} onClick={() => setOpen(false)}>Huỷ bỏ</Button>
                            <Button type="submit" variant='contained' sx={{ width: "50%" }}>Tạo</Button>
                        </Box>
                    </form>
                    <LoadingComponent open={loading} setLoading={setLoading} />
                </Box>
            </Modal>
        </div>
    )
}

export default UserCreation
