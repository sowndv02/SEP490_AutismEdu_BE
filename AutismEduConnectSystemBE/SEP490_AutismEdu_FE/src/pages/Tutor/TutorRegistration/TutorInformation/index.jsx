import DeleteIcon from '@mui/icons-material/Delete';
import RemoveRedEyeIcon from '@mui/icons-material/RemoveRedEye';
import { Box, Button, Divider, FormControl, FormHelperText, Grid, IconButton, MenuItem, Modal, Select, Stack, TextField, Typography } from '@mui/material';
import axios from 'axios';
import { useFormik } from 'formik';
import { useEffect, useRef, useState } from 'react';
import ModalUploadAvatar from './ModalUploadAvatar';
import ArrowBackIosIcon from '@mui/icons-material/ArrowBackIos';
import ArrowForwardIosIcon from '@mui/icons-material/ArrowForwardIos';
import HighlightOffIcon from '@mui/icons-material/HighlightOff';
import { enqueueSnackbar } from 'notistack';
function TutorInformation({ activeStep, handleBack, handleNext, steps, tutorInformation, setTutorInformation,
    IdVerification,
    setIdVerification }) {
    const [provinces, setProvinces] = useState([]);
    const [districts, setDistricts] = useState([]);
    const [communes, setCommunes] = useState([]);
    const [avatar, setAvatar] = useState();
    const [citizenIdentification, setCitizenIdentification] = useState([]);
    const [currentImage, setCurrentImage] = useState(null);
    const [inputKey, setInputKey] = useState(0);
    const [openDialog, setOpenDialog] = useState(false);
    const cIInput = useRef();
    const validate = values => {
        const errors = {};
        const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
        if (!values.email) {
            errors.email = "Bắt buộc"
        } else if (!emailRegex.test(values.email)) {
            errors.email = "Email của bạn không hợp lệ"
        } else if (values.email.length > 320) {
            errors.email = "Email phải dưới 320 kí tự"
        }
        if (!values.fullName) {
            errors.fullName = 'Bắt buộc';
        } else if (values.fullName.length > 50) {
            errors.fullName = 'Tên dưới 50 ký tự';
        } else if (!/^[a-zA-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂÊÔưăêôƠƯÀẢÃÁẠĂẮẰẲẴẶÂẦẤẨẪẬÈẺẼÉẸÊỀẾỂỄỆÌỈĨÍỊÒỎÕÓỌÔỒỐỔỖỘƠỜỚỞỠỢÙỦŨÚỤƯỪỨỬỮỰỲỶỸÝỴàảãáạăắằẳẵặâầấẩẫậèẻẽéẹêềếểễệìỉĩíịòỏõóọôồốổỗộơờớởỡợùủũúụưừứửữựỳỷỹýỵ\s]+$/.test(values.fullName)) {
            errors.fullName = 'Tên không hợp lệ'
        }
        if (!values.phoneNumber) {
            errors.phoneNumber = 'Bắt buộc';
        } else if (!/^(0?)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$/.test(values.phoneNumber)) {
            errors.phoneNumber = 'Số điện thoại không hợp lệ';
        }
        if (!values.dateOfBirth) {
            errors.dateOfBirth = 'Bắt buộc'
        }
        if (!values.province || !values.district || !values.commune || !values.homeNumber) {
            errors.address = 'Nhập đầy đủ địa chỉ';
        } else if (values.homeNumber.length > 100) {
            errors.address = 'Số nhà dưới 100 kí tự'
        }
        if (!values.identityCardNumber) {
            errors.identityCardNumber = "Bắt buộc"
        } else if (!/^\d{12}$/.test(values.identityCardNumber)) {
            errors.identityCardNumber = "Số CCCD không hợp lệ"
        }
        if (!values.issuingInstitution) {
            errors.issuingInstitution = "Bắt buộc"
        } else if (!values.issuingInstitution.length > 100) {
            errors.issuingInstitution = 'Nhỏ hơn 100 ký tự'
        }

        if (!values.issuingDate) {
            errors.issuingDate = "Bắt buộc"
        }
        if (!avatar) {
            errors.avatar = "Bắt buộc"
        }
        return errors;
    };
    const formik = useFormik({
        initialValues: {
            fullName: '',
            email: '',
            phoneNumber: '',
            dateOfBirth: '',
            province: '',
            district: '',
            commune: '',
            homeNumber: '',
            issuingInstitution: '',
            issuingDate: '',
            identityCardNumber: ''
        },
        validate,
        onSubmit: async (values) => {
            if (citizenIdentification.length < 2) {
                enqueueSnackbar("Chứng minh nhân dân phải đủ 2 mặt", { variant: "error" });
                return;
            }
            const dataTransfer = new DataTransfer();
            citizenIdentification.forEach(file => {
                dataTransfer.items.add(file);
            });

            setIdVerification({
                certificateName: "Căn cước công dân",
                issuingInstitution: values.issuingInstitution.trim(),
                issuingDate: values.issuingDate,
                identityCardNumber: values.identityCardNumber,
                medias: dataTransfer.files
            })
            const selectedCommune = communes.find(p => p.idCommune === values.commune);
            const selectedProvince = provinces.find(p => p.idProvince === values.province);
            const selectedDistrict = districts.find(p => p.idDistrict === values.district);
            setTutorInformation({
                image: avatar,
                fullName: values.fullName.trim(),
                email: values.email.trim(),
                phoneNumber: values.phoneNumber,
                dateOfBirth: values.dateOfBirth,
                province: selectedProvince || '',
                district: selectedDistrict || '',
                commune: selectedCommune || '',
                homeNumber: values.homeNumber.trim()
            })
            handleNext();
        }
    });
    useEffect(() => {
        getDataProvince();
        if (tutorInformation) {
            formik.setFieldValue("fullName", tutorInformation?.fullName || "");
            formik.setFieldValue("phoneNumber", tutorInformation?.phoneNumber || "");
            formik.setFieldValue("dateOfBirth", tutorInformation?.dateOfBirth || "");
            formik.setFieldValue("homeNumber", tutorInformation?.homeNumber || "");
            formik.setFieldValue("email", tutorInformation?.email || "");
            if (tutorInformation.image)
                setAvatar(tutorInformation.image)
        }
        if (IdVerification) {
            formik.setFieldValue("issuingInstitution", IdVerification?.issuingInstitution || "");
            formik.setFieldValue("issuingDate", IdVerification?.issuingDate || "");
            formik.setFieldValue("identityCardNumber", IdVerification?.identityCardNumber || "");
            if (IdVerification.medias) {
                setCitizenIdentification(Array.from(IdVerification.medias))
            }
        }
    }, [tutorInformation, IdVerification]);
    const getDataProvince = async () => {
        try {
            const data = await axios.get("https://vietnam-administrative-division-json-server-swart.vercel.app/province")
            const dataP = data.data;
            setProvinces(dataP);
            if (tutorInformation !== null) {
                const province = dataP.find((p) => { return p.idProvince === tutorInformation.province.idProvince });
                if (province) {
                    formik.setFieldValue("province", province.idProvince);
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
    const getMinDate = () => {
        const today = new Date();
        const year = today.getFullYear() - 70;
        const month = String(today.getMonth() + 1).padStart(2, '0');
        const day = String(today.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    };
    return (
        <>
            <form onSubmit={formik.handleSubmit}>
                <Typography variant='h3' textAlign="center" mt={3}>Thông tin cá nhân</Typography>
                <Grid container px="100px" py="50px" columnSpacing={2} rowSpacing={3}>
                    <Grid item xs={3} textAlign="right">Ảnh chân dung</Grid>
                    <Grid item xs={9}>
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
                    <Grid item xs={3} textAlign="right">Họ và tên gia sư</Grid>
                    <Grid item xs={9}>
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
                    <Grid item xs={3} textAlign="right">Email</Grid>
                    <Grid item xs={9}>
                        <TextField size='small' sx={{ width: "50%" }} onChange={formik.handleChange} name='email'
                            value={formik.values.email}
                        />
                        {
                            formik.errors.email && (
                                <FormHelperText error>
                                    {formik.errors.email}
                                </FormHelperText>
                            )
                        }
                    </Grid>
                    <Grid item xs={3} textAlign="right">Số điện thoại</Grid>
                    <Grid item xs={9}>
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
                    <Grid item xs={3} textAlign="right">Ngày sinh</Grid>
                    <Grid item xs={9}>
                        <TextField size='small' sx={{ width: "50%" }} type='date' inputProps={{
                            max: getMaxDate(),
                            min: getMinDate()
                        }}
                            value={formik.values.dateOfBirth}
                            onChange={formik.handleChange} name='dateOfBirth'
                        />
                        {
                            formik.errors.dateOfBirth && (
                                <FormHelperText error>
                                    {formik.errors.dateOfBirth}
                                </FormHelperText>
                            )
                        }
                    </Grid>
                    <Grid item xs={3} textAlign="right">Địa chỉ</Grid>
                    <Grid item xs={9}>
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
                        <Box mt={2}>
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
                        </Box>
                        <Box mt="20px">
                            <TextField label="Số nhà, Thôn" size='small' name='homeNumber' onChange={formik.handleChange}
                                value={formik.values.homeNumber}
                                sx={{ width: "70%" }} />
                            {
                                formik.errors.address && (
                                    <FormHelperText error>
                                        {formik.errors.address}
                                    </FormHelperText>
                                )
                            }
                        </Box>
                    </Grid>
                </Grid>
                <Divider>Căn cước công dân</Divider>
                <Grid container px="100px" py="50px" columnSpacing={2} rowSpacing={3}>
                    <Grid item xs={3} textAlign="right">Số căn cước công dân</Grid>
                    <Grid item xs={9}>
                        <TextField size='small' sx={{ width: "70%" }} fullWidth value={formik.values.identityCardNumber}
                            name='identityCardNumber'
                            onChange={formik.handleChange} />
                        {
                            formik.errors.identityCardNumber && (
                                <FormHelperText error>
                                    {formik.errors.identityCardNumber}
                                </FormHelperText>
                            )
                        }
                    </Grid>
                    <Grid item xs={3} textAlign="right">Nơi cấp</Grid>
                    <Grid item xs={9}>
                        <TextField size='small' sx={{ width: "70%" }} fullWidth value={formik.values.issuingInstitution}
                            name='issuingInstitution'
                            onChange={formik.handleChange} />
                        {
                            formik.errors.issuingInstitution && (
                                <FormHelperText error>
                                    {formik.errors.issuingInstitution}
                                </FormHelperText>
                            )
                        }
                    </Grid>
                    <Grid item xs={3} textAlign="right">Ngày cấp</Grid>
                    <Grid item xs={9}>
                        <TextField size='small' sx={{ width: "70%" }} fullWidth value={formik.values.issuingDate}
                            name='issuingDate'
                            onChange={formik.handleChange}
                            type='date'
                            inputProps={{
                                max: new Date().toISOString().split('T')[0],
                                min: getMinDate()
                            }}
                        />
                        {
                            formik.errors.issuingDate && (
                                <FormHelperText error>
                                    {formik.errors.issuingDate}
                                </FormHelperText>
                            )
                        }
                    </Grid>
                    <Grid item xs={3} textAlign="right">Hình ảnh chụp của thẻ CCCD <Typography>(mặt trước và mặt sau)</Typography> </Grid>
                    <Grid item xs={9}>
                        <TextField size='small' type='file' sx={{ width: "70%" }}
                            onChange={(e) => {
                                if (e.target.files.length > 2) {
                                    enqueueSnackbar("Chỉ chọn 2 ảnh", { variant: "error" });
                                    e.target.value = "";
                                } else if (e.target.files.length > 1 && citizenIdentification.length >= 1) {
                                    enqueueSnackbar("Chỉ chọn 2 ảnh", { variant: "error" });
                                    e.target.value = "";
                                }
                                else {
                                    setCitizenIdentification([...citizenIdentification, ...Array.from(e.target.files)])
                                    setInputKey(preKey => preKey + 1)
                                }
                            }}
                            inputProps={{
                                multiple: true,
                                accept: "image/png, image/jpeg"
                            }}
                            key={inputKey}
                            ref={cIInput}
                            disabled={citizenIdentification.length === 2}
                        />
                        {
                            citizenIdentification?.length === 0 && (
                                <FormHelperText error>
                                    Bắt buộc
                                </FormHelperText>
                            )
                        }
                        {
                            citizenIdentification?.length === 1 && (
                                <FormHelperText error>
                                    Chụp đủ 2 mặt
                                </FormHelperText>
                            )
                        }
                        <Stack direction="row" gap={2} flexWrap="wrap">
                            {
                                citizenIdentification && citizenIdentification.map((image, index) => {
                                    return (
                                        <Box mt={2} sx={{
                                            width: '100px', height: "100px", position: "relative",
                                            overflow: "hidden",
                                            ":hover": {
                                                ".overlay-image": {
                                                    width: "100%",
                                                    height: "100%",
                                                    position: 'absolute',
                                                    top: "0",
                                                    left: "0",
                                                    bgcolor: "#676b7b5e",
                                                    display: "flex",
                                                    justifyContent: 'center',
                                                    alignItems: 'center'
                                                }
                                            }
                                        }} key={index}>
                                            <img src={URL.createObjectURL(image)} alt="Preview" style={{ width: '100%', height: "100%" }} />
                                            <Box sx={{ display: "none" }} className="overlay-image">
                                                <RemoveRedEyeIcon sx={{ color: "white", cursor: "pointer" }}
                                                    onClick={() => { setOpenDialog(true), setCurrentImage(index) }} />
                                                <DeleteIcon sx={{ color: "white", cursor: "pointer" }} onClick={() => {
                                                    const fArray = citizenIdentification.filter((img, i) => {
                                                        return i !== index;
                                                    })
                                                    setCitizenIdentification(fArray)
                                                }} />
                                            </Box>
                                        </Box>
                                    )
                                })
                            }
                        </Stack>
                    </Grid>
                </Grid>
                {
                    currentImage !== null && (
                        <Modal open={openDialog} onClose={() => setOpenDialog(false)}>
                            <Box
                                display="flex"
                                justifyContent="center"
                                alignItems="center"
                                height="100vh"
                                bgcolor="rgba(0, 0, 0, 0.8)"
                                position="relative"
                            >
                                <img
                                    src={URL.createObjectURL(citizenIdentification[currentImage])}
                                    alt="large"
                                    style={{ maxWidth: '90%', maxHeight: '90%' }}
                                />

                                <IconButton
                                    onClick={() => setOpenDialog(false)}
                                    style={{ position: 'absolute', top: 20, right: 20, color: 'white' }}
                                >
                                    <HighlightOffIcon />
                                </IconButton>
                                <IconButton
                                    style={{ position: 'absolute', left: 20, color: 'white' }}
                                    onClick={() => setCurrentImage(currentImage === 0 ? 0 : currentImage - 1)}
                                >
                                    <ArrowBackIosIcon />
                                </IconButton>
                                <IconButton
                                    style={{ position: 'absolute', right: 20, color: 'white' }}
                                    onClick={() => setCurrentImage(currentImage === citizenIdentification.length - 1 ? currentImage : currentImage + 1)}
                                >
                                    <ArrowForwardIosIcon />
                                </IconButton>
                            </Box>
                        </Modal>
                    )
                }
                <Box sx={{ display: 'flex', flexDirection: 'row', pt: 2 }}>
                    <Button
                        color="inherit"
                        disabled={activeStep === 0}
                        onClick={handleBack}
                        sx={{ mr: 1 }}
                    >
                        Quay lại
                    </Button>
                    <Box sx={{ flex: '1 1 auto' }} />
                    <Button type="submit">
                        {activeStep === steps.length - 1 ? 'Kết thúc' : 'Tiếp theo'}
                    </Button>
                </Box>
            </form>
        </>
    )
}

export default TutorInformation
