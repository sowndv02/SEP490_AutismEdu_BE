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
const ITEM_HEIGHT = 48;
const ITEM_PADDING_TOP = 8;
const MenuProps = {
    PaperProps: {
        style: {
            maxHeight: ITEM_HEIGHT * 4.5 + ITEM_PADDING_TOP,
            width: 250,
        }
    }
};
function UserCreation({ setUsers }) {
    const [open, setOpen] = React.useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const [showPassword, setShowPassword] = useState(false);
    const [loading, setLoading] = useState(true);
    const [roles, setRoles] = useState([]);
    const [selectedRoles, setSelectedRoles] = useState([]);
    const validate = values => {
        const errors = {};
        if (!values.fullName) {
            errors.fullName = 'Required';
        } else if (values.fullName.length > 15) {
            errors.fullName = 'Must be 15 characters or less';
        }
        if (!values.email) {
            errors.email = 'Required'
        } else if (!/^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/.test(values.email)) {
            errors.email = 'Unvalid email';
        }
        return errors;
    };

    useEffect(() => {
        if (open) {
            getRoles();
        } else {
            formik.values.fullName = "";
            formik.values.email = '';
            formik.values.phoneNumber = '';
            formik.values.password = '';
            formik.values.cfPassword = ''
            setSelectedRoles([])
        }
    }, [open]);
    const getRoles = async () => {
        try {
            await services.RoleManagementAPI.getRoles((res) => {
                const returnRole = res.result.filter((r) => {
                    return r.name !== "User"
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
    const handleMouseDownPassword = (event) => {
        event.preventDefault();
    };

    const handleChange = (event) => {
        const {
            target: { value },
        } = event;
        setSelectedRoles(
            typeof value === 'string' ? value.split(',') : value
        );
    };
    const handleClickShowPassword = () => setShowPassword((show) => !show);
    const formik = useFormik({
        initialValues: {
            fullName: '',
            email: '',
            phoneNumber: '',
            password: '',
            cfPassword: ''
        },
        validate,
        onSubmit: async (values) => {
            try {
                setLoading(true);
                const submitRoles = selectedRoles.map((role) => {
                    const selectedRole = roles.find((r) => r.name === role);
                    return selectedRole.id;
                })
                setOpen(false);
                await services.UserManagementAPI.createUser(
                    {
                        FullName: values.fullName,
                        Email: values.email,
                        PhoneNumber: values.phoneNumber,
                        Password: values.password,
                        ConfirmPassword: values.cfPassword,
                        RoleIds: submitRoles,
                        IsLockedOut: false
                    }
                    , (res) => {
                        console.log(res);
                        let splitedRole = res.result.role.split(",");
                        res.result.role = splitedRole;
                        setUsers(preState => [res.result, ...preState]);
                        enqueueSnackbar("Create account successfully!", { variant: "success" });
                    }, (error) => {
                        console.log(error);
                        if (error.code === 400)
                            enqueueSnackbar("Email has already exist!", { variant: "error" });
                        else
                            enqueueSnackbar("Failed to create account!", { variant: "error" });
                    })
                setLoading(false);
            } catch (error) {
                console.log(error);
                setLoading(false);
            }
        },
    });
    console.log(open);
    return (
        <div>
            <Button variant="contained" onClick={handleOpen}>Add new user</Button>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style}>
                    <Typography id="modal-modal-title" variant="h6" sx={{ color: "black", fontWeight: "bold" }}>
                        Create New Account
                    </Typography>
                    <Divider />
                    <form onSubmit={formik.handleSubmit}>
                        <Box sx={{ display: "flex", gap: "10px", mt: "20px" }}>
                            <FormControl sx={{ width: '50%' }} variant="outlined">
                                <label style={{ fontWeight: "bold", color: "black", fontSize: "14px" }} htmlFor="fullName">Full name</label>
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
                            <label style={{ fontWeight: "bold", color: "black", fontSize: "14px" }} htmlFor="phone">Phone Number</label>
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
                            <label style={{ fontWeight: "bold", color: "black", fontSize: "14px" }} htmlFor="password">Password</label>
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
                            <label style={{ fontWeight: "bold", color: "black", fontSize: "14px" }} htmlFor="cfPassword">Confirm Password</label>
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
                                <label style={{ fontWeight: "bold", color: "black", fontSize: "14px" }} htmlFor="role">Roles</label>
                                <Select
                                    labelId="roles"
                                    id="multiple-roles"
                                    multiple
                                    value={selectedRoles}
                                    onChange={handleChange}
                                    input={<OutlinedInput label="" />}
                                    renderValue={(selected) => selected.join(', ')}
                                    MenuProps={MenuProps}
                                >
                                    {roles?.map((role) => (
                                        <MenuItem key={role.id} value={role.name}>
                                            <Checkbox checked={selectedRoles.indexOf(role.name) > -1} />
                                            <ListItemText primary={role.name} />
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                        </Box>
                        <Box sx={{ display: "flex", gap: "10px", mt: "20px" }}>
                            <Button variant='outlined' color='inherit' sx={{ width: "50%" }} onClick={() => setOpen(false)}>Close</Button>
                            <Button type="submit" variant='contained' sx={{ width: "50%" }}>Create</Button>
                        </Box>
                    </form>
                    <LoadingComponent open={loading} setLoading={setLoading} />
                </Box>
            </Modal>
        </div>
    )
}

export default UserCreation
