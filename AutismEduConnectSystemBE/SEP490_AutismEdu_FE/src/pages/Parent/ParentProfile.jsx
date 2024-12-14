import { Avatar, Box, Button, FormHelperText, Grid, MenuItem, Select, TextField } from '@mui/material';
import axios from 'axios';
import { useFormik } from 'formik';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import axiosInstance from "~/plugins/axiosConfig";
import services from '~/plugins/services';
import { setUserInformation, userInfor } from '~/redux/features/userSlice';
import ModalUploadAvatar from '../Tutor/TutorRegistration/TutorInformation/ModalUploadAvatar';
function ParentProfile() {
    const [provinces, setProvinces] = useState([]);
    const [districts, setDistricts] = useState([]);
    const [communes, setCommunes] = useState([]);
    const [avatar, setAvatar] = useState();
    const userInformation = useSelector(userInfor);
    const [change, setChange] = useState(true);
    const dispatch = useDispatch();
    const validate = (values) => {
        const errors = {};
        if (!values.fullName) {
            errors.fullName = 'Bắt buộc';
        } else if (values.fullName.length > 50) {
            errors.fullName = 'Tên dưới 50 ký tự';
        } else if (!/^[a-zA-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂÊÔưăêôƠƯÀẢÃÁẠĂẮẰẲẴẶÂẦẤẨẪẬÈẺẼÉẸÊỀẾỂỄỆÌỈĨÍỊÒỎÕÓỌÔỒỐỔỖỘƠỜỚỞỠỢÙỦŨÚỤƯỪỨỬỮỰỲỶỸÝỴàảãáạăắằẳẵặâầấẩẫậèẻẽéẹêềếểễệìỉĩíịòỏõóọôồốổỗộơờớởỡợùủũúụưừứửữựỳỷỹýỵ\s]+$/.test(values.fullName)) {
            errors.fullName = "Tên không hợp lệ!"
        }
        if (!values.phoneNumber) {
            errors.phoneNumber = 'Bắt buộc';
        } else if (!/^(0?)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$/.test(values.phoneNumber)) {
            errors.phoneNumber = 'Số điện thoại không hợp lệ';
        }
        if (!values.province) {
            errors.address = 'Bắt buộc';
        }
        if (!values.district) {
            errors.address = 'Bắt buộc';
        }
        if (!values.commune) {
            errors.address = 'Bắt buộc';
        }
        if (!values.homeNumber) {
            errors.address = 'Bắt buộc';
        } else if (values.homeNumber.length > 100) {
            errors.address = 'Phải dưới 100 ký tự'
        }
        return errors;
    }
    const formik = useFormik({
        initialValues: {
            fullName: '',
            province: '',
            district: '',
            commune: '',
            homeNumber: '',
            phoneNumber: ''
        },
        validate,
        onSubmit: async (values) => {
            const selectedCommune = communes.find(p => p.idCommune === values.commune);
            const selectedProvince = provinces.find(p => p.idProvince === values.province);
            const selectedDistrict = districts.find(p => p.idDistrict === values.district);
            const formData = new FormData();
            formData.append("FullName", values.fullName.trim())
            formData.append("PhoneNumber", values.phoneNumber)
            formData.append("Address", `${selectedProvince.name}|${selectedDistrict.name}|${selectedCommune.name}|${values.homeNumber.trim()}`)
            if (avatar) {
                formData.append("Image", avatar)
            }
            axiosInstance.setHeaders({ "Content-Type": "multipart/form-data", "Accept": "application/json, text/plain, multipart/form-data, */*" });
            await services.UserManagementAPI.updateUser(userInformation.id, formData, (res) => {
                enqueueSnackbar("Cập nhật hồ sơ thành công", { variant: "success" });
                dispatch(setUserInformation(res.result))
            }, (err) => {
                enqueueSnackbar(err.error[0], { variant: "error" });
            })
            axiosInstance.setHeaders({ "Content-Type": "application/json", "Accept": "application/json, text/plain, */*" });
        }
    });

    useEffect(() => {
        getDataProvince();
        if (userInformation) {
            formik.resetForm({
                values: {
                    fullName: userInformation?.fullName || "",
                    phoneNumber: userInformation?.phoneNumber || "",
                    dateOfBirth: userInformation?.dateOfBirth || "",
                    homeNumber: userInformation?.address?.split("|")[3] || "",
                    province: "",
                    district: "",
                    commune: ""
                }
            })
            if (userInformation.image)
                setAvatar(userInformation.image)
        }
    }, [userInformation]);
    const getDataProvince = async () => {
        try {
            const data = await axios.get("https://vietnam-administrative-division-json-server-swart.vercel.app/province");
            const dataP = data.data;
            setProvinces(dataP);
            if (userInformation !== null && userInformation.address) {
                const address = userInformation.address.split('|');
                const province = dataP.find((p) => { return p.name === address[0] });
                if (province) {
                    formik.setFieldValue("province", province.idProvince);
                    const dataD = await handleGetDistrict(province.idProvince);
                    const district = dataD.find((d) => { return d.name === address[1] });
                    if (district) {
                        formik.setFieldValue("district", district.idDistrict);
                        const dataC = await handleGetCommunes(district.idDistrict);
                        const commune = dataC.find((c) => { return c.name === address[2] });
                        if (commune) {
                            formik.setFieldValue("commune", commune.idCommune);
                        }
                    }
                }
            }
        } catch (error) {
            console.log(error);
        }
    };


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

    useEffect(() => {
        if (userInformation && provinces.length !== 0 && districts.length !== 0 && communes.length !== 0) {
            const fullName = formik.values.fullName.trim();
            if (avatar) {
                setChange(false);
                return;
            }
            if (fullName !== userInformation.fullName) {
                setChange(false);
                return;
            }
            if (formik.values.phoneNumber.trim() !== userInformation.phoneNumber) {
                setChange(false);
                return;
            }
            const selectedProvince = provinces.find((p) => {
                return p.idProvince === formik.values.province;
            })
            if (!userInformation.address && formik.values.district && formik.values.province
                && formik.values.commune
            ) {
                setChange(false);
                return;
            }
            if (userInformation.address && provinces && districts && communes && selectedProvince) {
                const address = userInformation.address.split("|");
                if (selectedProvince.name !== address[0]) {
                    setChange(false);
                    return;
                }
                const selectedDistrict = districts.find((d) => {
                    return d.idDistrict === formik.values.district;
                })
                if (selectedDistrict && (selectedDistrict.name !== address[1])) {
                    setChange(false);
                    return;
                }
                const selectedCommune = communes.find((c) => {
                    return c.idCommune === formik.values.commune;
                })
                if (selectedCommune && (selectedCommune.name !== address[2])) {
                    setChange(false);
                    return;
                }
                if (formik.values.homeNumber?.trim() !== address[3]) {
                    setChange(false);
                    return;
                }
            }
            setChange(true);
        }
    }, [formik])

    return (
        <Box sx={{ bgcolor: "#efefef", width: "100%", py: "20px" }}>
            <Box sx={{
                width: "80%", m: "auto", pt: '50px', bgcolor: "white",
                borderRadius: "10px",
                boxShadow: "rgba(0, 0, 0, 0.35) 0px 5px 15px"
            }}>
                <Box sx={{ m: "auto", textAlign: "center" }}>
                    {
                        avatar ? (<Avatar src={URL.createObjectURL(avatar)} alt="Remy Sharp" sx={{ m: "auto", width: "100px", height: "100px", mb: "20px" }} />)
                            : (<Avatar src={userInformation?.imageUrl} alt="Remy Sharp" sx={{ m: "auto", width: "100px", height: "100px", mb: "20px" }} />)
                    }
                    <ModalUploadAvatar setAvatar={setAvatar} />
                </Box>
                <form onSubmit={formik.handleSubmit}>
                    <Grid container px="100px" py="50px" columnSpacing={2} rowSpacing={3}>
                        <Grid item xs={4} textAlign="right">Họ và tên</Grid>
                        <Grid item xs={8}>
                            <TextField size='small' sx={{ width: "50%" }}
                                value={formik.values.fullName}
                                onChange={formik.handleChange} name='fullName' />
                            {
                                formik.errors.fullName && (
                                    <FormHelperText error>
                                        {formik.errors.fullName}
                                    </FormHelperText>
                                )
                            }
                        </Grid>
                        <Grid item xs={4} textAlign="right">Số điện thoại</Grid>
                        <Grid item xs={8}>
                            <TextField size='small' sx={{ width: "50%" }} onChange={formik.handleChange} name='phoneNumber'
                                value={formik.values.phoneNumber}
                            />
                            {
                                formik.errors.phoneNumber && (
                                    <FormHelperText error>
                                        {formik.errors.phoneNumber}
                                    </FormHelperText>
                                )
                            }
                        </Grid>
                        <Grid item xs={4} textAlign="right">Địa chỉ</Grid>
                        <Grid item xs={8}>
                            <Select
                                value={formik.values.province}
                                name='province'
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
                                renderValue={(selected) => {
                                    if (!selected || selected === "") {
                                        return <em>Tỉnh / TP</em>;
                                    }
                                    const selectedProvince = provinces.find(p => p.idProvince === selected);
                                    return selectedProvince ? selectedProvince.name : "";
                                }}
                                displayEmpty={true}
                                size='small'
                            >
                                <MenuItem disabled value="">
                                    <em>Tỉnh / TP</em>
                                </MenuItem>
                                {
                                    provinces.length !== 0 && provinces?.map((province) => {
                                        return (
                                            <MenuItem value={province?.idProvince} key={province?.idProvince}>{province.name}</MenuItem>
                                        )
                                    })
                                }
                            </Select>
                            <Select
                                value={formik.values.district}
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
                                displayEmpty={true}
                                disabled={districts.length === 0}
                                size='small'
                                sx={{ ml: "20px" }}
                            >
                                <MenuItem disabled value="">
                                    <em>Quận / Huyện</em>
                                </MenuItem>
                                {
                                    districts.length !== 0 && districts?.map((district) => {
                                        return (
                                            <MenuItem value={district?.idDistrict} key={district?.idDistrict}>{district.name}</MenuItem>
                                        )
                                    })
                                }
                            </Select>
                            <Select
                                labelId="demo-simple-select-label"
                                id="demo-simple-select"
                                value={formik.values.commune}
                                name='commune'
                                onChange={formik.handleChange}
                                renderValue={(selected) => {
                                    if (!selected || selected === "") {
                                        return <em>Xã / Phường</em>;
                                    }
                                    const selectedCommune = communes.find(p => p.idCommune === selected);
                                    return selectedCommune ? selectedCommune.name : <em>Xã / Phường</em>;
                                }}
                                displayEmpty={true}
                                disabled={communes.length === 0}
                                size='small'
                                sx={{ ml: "20px" }}
                            >
                                <MenuItem disabled value="">
                                    <em>Xã / Phường</em>
                                </MenuItem>
                                {
                                    communes.length !== 0 && communes?.map((commune) => {
                                        return (
                                            <MenuItem value={commune?.idCommune} key={commune?.idCommune}>{commune.name}</MenuItem>
                                        )
                                    })
                                }
                            </Select>
                            <Box mt="20px">
                                <TextField label="Số nhà, Thôn" size='small' name='homeNumber' onChange={formik.handleChange}
                                    value={formik.values.homeNumber}
                                    sx={{ width: "60%" }} />
                                {
                                    formik.errors.address && (
                                        <FormHelperText error>
                                            {formik.errors.address}
                                        </FormHelperText>
                                    )
                                }
                            </Box>
                            <Button variant='contained' sx={{ mt: 3 }} disabled={change} type='submit'>Lưu</Button>
                        </Grid>
                    </Grid>
                </form>
            </Box>
        </Box>
    )
}

export default ParentProfile
