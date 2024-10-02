import DeleteIcon from '@mui/icons-material/Delete';
import RemoveRedEyeIcon from '@mui/icons-material/RemoveRedEye';
import { Box, Button, FormHelperText, Grid, MenuItem, Modal, Select, TextField, Typography } from '@mui/material';
import axios from 'axios';
import { useFormik } from 'formik';
import { useEffect, useState } from 'react';
import ModalUploadAvatar from './ModalUploadAvatar';
function TutorInformation({ activeStep, handleBack, handleNext, steps, tutorInformation, setTutorInformation }) {
    const [provinces, setProvinces] = useState([]);
    const [districts, setDistricts] = useState([]);
    const [communes, setCommunes] = useState([]);
    const [avatar, setAvatar] = useState();
    const validate = values => {
        const errors = {};
        if (!values.formalName) {
            errors.formalName = 'Bắt buộc';
        } else if (values.formalName.length > 20) {
            errors.formalName = 'Tên dưới 20 ký tự';
        }
        if (!values.phoneNumber) {
            errors.phoneNumber = 'Bắt buộc';
        } else if (!/^(0?)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$/.test(values.phoneNumber)) {
            errors.phoneNumber = 'Số điện thoại không hợp lệ';
        }

        if (!values.province || !values.district || !values.commune || !values.homeNumber) {
            errors.address = 'Nhập đầy đủ địa chỉ';
        }
        if (!values.startAge || !values.endAge) {
            errors.rangeAge = 'Vui lòng nhập độ tuổi';
        } else if (values.startAge > values.endAge) {
            errors.rangeAge = 'Độ tuổi không hợp lệ';
        }
        if (!avatar) {
            errors.avatar = "Bắt buộc"
        }
        return errors;
    };
    const formik = useFormik({
        initialValues: {
            formalName: '',
            phoneNumber: '',
            dateOfBirth: '',
            province: '',
            district: '',
            commune: '',
            homeNumber: '',
            startAge: '',
            endAge: ''
        },
        validate,
        onSubmit: async (values) => {
            const selectedCommune = communes.find(p => p.idCommune === values.commune);
            const selectedProvince = provinces.find(p => p.idProvince === values.province);
            const selectedDistrict = districts.find(p => p.idDistrict === values.district);
            setTutorInformation({
                avatar: avatar,
                formalName: values.formalName,
                phoneNumber: values.phoneNumber,
                dateOfBirth: values.dateOfBirth,
                province: selectedProvince || '',
                district: selectedDistrict || '',
                commune: selectedCommune || '',
                homeNumber: values.homeNumber,
                startAge: values.startAge,
                endAge: values.endAge
            })
            handleNext();
        }
    });
    useEffect(() => {
        getDataProvince();
        if (tutorInformation) {
            formik.setFieldValue("formalName", tutorInformation?.formalName || "");
            formik.setFieldValue("phoneNumber", tutorInformation?.phoneNumber || "");
            formik.setFieldValue("dateOfBirth", tutorInformation?.dateOfBirth || "");
            formik.setFieldValue("homeNumber", tutorInformation?.homeNumber || "");
            formik.setFieldValue("startAge", tutorInformation?.startAge || "");
            formik.setFieldValue("endAge", tutorInformation?.endAge || "");
        }
    }, [tutorInformation]);
    const getDataProvince = async () => {
        try {
            const data = await axios.get("https://vietnam-administrative-division-json-server-swart.vercel.app/province")
            const dataP = data.data;
            setProvinces(dataP);
            if (tutorInformation !== null) {
                const province = dataP.find((p) => { return p.idProvince === tutorInformation.province.idProvince });
                if (province) {
                    formik.setFieldValue("province", province.idProvince);
                    handleGetDistrict(districts);
                    const dataD = await handleGetDistrict(province.idProvince);
                    const district = dataD.find((d) => { return d.idDistrict === tutorInformation.district.idDistrict });
                    if (district) {
                        formik.setFieldValue("district", district.idDistrict);
                        const dataC = await handleGetCommunes(district.idDistrict);
                        const commune = dataC.find((c) => { return c.idCommune === tutorInformation.commune.idCommune });
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
            console.log(id);
            const data = await axios.get("https://vietnam-administrative-division-json-server-swart.vercel.app/commune?idDistrict=" + id);
            setCommunes(data.data);
            return data.data
        } catch (error) {
            console.log(error);
        }
    }
    const getMaxDate = () => {
        const today = new Date();
        const year = today.getFullYear() - 20;
        const month = String(today.getMonth() + 1).padStart(2, '0');
        const day = String(today.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    };

    return (
        <>
            <form onSubmit={formik.handleSubmit}>
                <Grid container px="100px" py="50px" columnSpacing={2} rowSpacing={3}>
                    <Grid item xs={2} textAlign="right">Ảnh chân dung</Grid>
                    <Grid item xs={10}>
                        <ModalUploadAvatar setAvatar={setAvatar} />
                        {
                            !avatar && <FormHelperText error>
                                Bắt buộc
                            </FormHelperText>
                        }
                        <Box>
                            {
                                avatar &&
                                <img src={URL.createObjectURL(avatar)} alt='avatar' width={150} />
                            }
                        </Box>
                    </Grid>
                    <Grid item xs={2} textAlign="right">Họ và tên gia sư</Grid>
                    <Grid item xs={10}>
                        <TextField size='small' sx={{ width: "50%" }}
                            value={formik.values.formalName}
                            onChange={formik.handleChange} name='formalName' />
                        {
                            formik.errors.formalName && (
                                <FormHelperText error>
                                    {formik.errors.formalName}
                                </FormHelperText>
                            )
                        }
                    </Grid>
                    <Grid item xs={2} textAlign="right">Email</Grid>
                    <Grid item xs={10}>
                        <Typography>daoquangkhai200@gmail.com</Typography>
                    </Grid>
                    <Grid item xs={2} textAlign="right">Số điện thoại</Grid>
                    <Grid item xs={10}>
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
                    <Grid item xs={2} textAlign="right">Ngày sinh</Grid>
                    <Grid item xs={10}>
                        <TextField size='small' sx={{ width: "50%" }} type='date' inputProps={{ max: getMaxDate() }}
                            value={formik.values.dateOfBirth}
                            onChange={formik.handleChange} name='dateOfBirth'
                        />
                    </Grid>
                    <Grid item xs={2} textAlign="right">Địa chỉ</Grid>
                    <Grid item xs={10}>
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
                                console.log(event.values);
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
                                fullWidth />
                            {
                                formik.errors.address && (
                                    <FormHelperText error>
                                        {formik.errors.address}
                                    </FormHelperText>
                                )
                            }
                        </Box>
                    </Grid>
                    <Grid item xs={2} textAlign="right">Độ tuổi dạy</Grid>
                    <Grid item xs={10}>
                        <TextField size='small' label="Từ" sx={{ mr: "20px" }} type='number' inputProps={{ min: 0, max: 15 }}
                            name='startAge'
                            value={formik.values.startAge}
                            onChange={(e) => {
                                const value = e.target.value;
                                if (Number.isInteger(Number(value)) || value === '') {
                                    formik.setFieldValue('startAge', value);
                                }
                            }}
                        />
                        <TextField size='small' label="Đến" type='number' inputProps={{ min: 0, max: 15 }}
                            name='endAge'
                            value={formik.values.endAge}
                            onChange={(e) => {
                                const value = e.target.value;
                                if (Number.isInteger(Number(value)) || value === '') {
                                    formik.setFieldValue('endAge', value);
                                }
                            }} />
                        {
                            formik.errors.rangeAge && (
                                <FormHelperText error>
                                    {formik.errors.rangeAge}
                                </FormHelperText>
                            )
                        }
                    </Grid>
                </Grid>
                <Box sx={{ display: 'flex', flexDirection: 'row', pt: 2 }}>
                    <Button
                        color="inherit"
                        disabled={activeStep === 0}
                        onClick={handleBack}
                        sx={{ mr: 1 }}
                    >
                        Back
                    </Button>
                    <Box sx={{ flex: '1 1 auto' }} />
                    <Button type="submit">
                        {activeStep === steps.length - 1 ? 'Finish' : 'Next'}
                    </Button>
                </Box>
            </form>
        </>
    )
}

export default TutorInformation
