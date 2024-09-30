import { Box, Button, FormHelperText, Grid, MenuItem, Select, TextField, Typography } from '@mui/material';
import { useFormik } from 'formik';
import { useEffect, useState } from 'react';
import axios from 'axios';

function TutorInformation({ activeStep, handleBack, handleNext, steps, tutorInformation, setTutorInformation }) {
    const [provinces, setProvinces] = useState([]);
    const [districts, setDistricts] = useState([]);
    const [communes, setCommunes] = useState([]);
    useEffect(() => {
        getDataProvince();
    }, []);
    const getDataProvince = async () => {
        try {
            const data = await axios.get("https://vietnam-administrative-division-json-server-swart.vercel.app/province")
            const dataP = data.data;
            setProvinces(dataP);
            if (tutorInformation !== null) {
                let address = tutorInformation.address.split("|");
                const province = dataP.find((p) => { return p.name === address[0] });
                if (province) {
                    formik.setFieldValue("province", province.idProvince);
                    handleGetDistrict(districts);
                    const dataD = await handleGetDistrict(province.idProvince);
                    const district = dataD.find((d) => { return d.name === address[1] });
                    if (district) {
                        formik.setFieldValue("district", district.idDistrict);
                        const dataC = await handleGetCommunes(district.idDistrict);
                        const commune = dataC.find((c) => { return c.name === address[2] });
                        if (commune) {
                            formik.setFieldValue("ward", commune.idCommune);
                            formik.setFieldValue("homeNumber", address[3]);
                        }
                    }
                }
            }
        } catch (error) {
            console.log(error);
        }
    };

    const validate = values => {
        const errors = {};
        if (!values.fullName) {
            errors.fullName = 'Bắt buộc';
        } else if (values.fullName.length > 20) {
            errors.fullName = 'Tên dưới 20 ký tự';
        }
        if (!values.phoneNumber) {
            errors.phoneNumber = 'Bắt buộc';
        } else if (!/^(0?)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$/.test(values.phoneNumber)) {
            errors.phoneNumber = 'Số điện thoại không hợp lệ';
        }

        if (!values.province || !values.district || !values.ward || !values.homeNumber) {
            errors.address = 'Nhập đầy đủ địa chỉ';
        }
        if (!values.fromAge || !values.toAge) {
            errors.rangeAge = 'Vui lòng nhập độ tuổi';
        } else if (values.fromAge > values.toAge) {
            errors.rangeAge = 'Độ tuổi không hợp lệ';
        }
        return errors;
    };

    const handleGetDistrict = async (id) => {
        try {
            const data = await axios.get("https://vietnam-administrative-division-json-server-swart.vercel.app/district?idProvince=" + id);
            setDistricts(data.data);
            return data.data
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
    const formik = useFormik({
        initialValues: {
            fullName: tutorInformation?.fullName || '',
            phoneNumber: tutorInformation?.phoneNumber || '',
            birthDate: tutorInformation?.birthDate || '',
            province: '',
            district: '',
            ward: '',
            homeNumber: '',
            fromAge: tutorInformation?.fromAge || '',
            toAge: tutorInformation?.toAge || ''
        },
        validate,
        onSubmit: async (values) => {
            const selectedCommune = communes.find(p => p.idCommune === values.ward);
            const selectedProvince = provinces.find(p => p.idProvince === values.province);
            const selectedDistrict = districts.find(p => p.idDistrict === values.district);
            let address = `${selectedProvince.name}|${selectedDistrict.name}|${selectedCommune.name}|${values.homeNumber}`
            setTutorInformation({
                fullName: values.fullName,
                phoneNumber: values.phoneNumber,
                birthDate: values.birthDate,
                address: address,
                fromAge: values.fromAge,
                toAge: values.toAge
            })
            handleNext();
        }
    });

    console.log(districts);
    return (
        <>
            <form onSubmit={formik.handleSubmit}>
                <Grid container px="100px" py="50px" columnSpacing={2} rowSpacing={3}>
                    <Grid item xs={2} textAlign="right">Họ và tên gia sư</Grid>
                    <Grid item xs={10}>
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
                            value={formik.values.birthDate}
                            onChange={formik.handleChange} name='birthDate'
                        />
                    </Grid>
                    <Grid item xs={2} textAlign="right">Địa chỉ</Grid>
                    <Grid item xs={10}>
                        <Select
                            value={formik.values.province}
                            name='province'
                            onChange={(event) => {
                                formik.handleChange(event);
                                handleGetDistrict(event.target.value);
                                setCommunes([]);
                                formik.setFieldValue('district', '')
                                formik.setFieldValue('ward', '')
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
                                formik.setFieldValue('ward', '')
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
                            value={formik.values.ward}
                            name='ward'
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
                            name='fromAge'
                            value={formik.values.fromAge}
                            onChange={(e) => {
                                const value = e.target.value;
                                if (Number.isInteger(Number(value)) || value === '') {
                                    formik.setFieldValue('fromAge', value);
                                }
                            }}
                        />
                        <TextField size='small' label="Đến" type='number' inputProps={{ min: 0, max: 15 }}
                            name='toAge'
                            value={formik.values.toAge}
                            onChange={(e) => {
                                const value = e.target.value;
                                if (Number.isInteger(Number(value)) || value === '') {
                                    formik.setFieldValue('toAge', value);
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
