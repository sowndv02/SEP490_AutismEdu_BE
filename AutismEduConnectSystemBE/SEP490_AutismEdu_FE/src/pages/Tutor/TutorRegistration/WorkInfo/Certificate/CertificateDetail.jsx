import ArrowBackIosIcon from '@mui/icons-material/ArrowBackIos';
import ArrowForwardIosIcon from '@mui/icons-material/ArrowForwardIos';
import DeleteIcon from '@mui/icons-material/Delete';
import HighlightOffIcon from '@mui/icons-material/HighlightOff';
import RemoveRedEyeIcon from '@mui/icons-material/RemoveRedEye';
import SchoolIcon from '@mui/icons-material/School';
import { FormHelperText, Grid, IconButton, ListItemButton, ListItemIcon, Stack, TextField } from '@mui/material';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Modal from '@mui/material/Modal';
import Typography from '@mui/material/Typography';
import { useFormik } from 'formik';
import { enqueueSnackbar } from 'notistack';
import * as React from 'react';
import ConfirmDeleteDialog from './ConfirmDeleteDialog';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 800,
    maxHeight: "90vh",
    bgcolor: 'background.paper',
    boxShadow: 24,
    overflowY: "auto",
    p: 4,
};

export default function CertificateDetail({ certificate, setCertificate, index, currentItem }) {
    const [open, setOpen] = React.useState(false);
    const [openDialog, setOpenDialog] = React.useState(false);
    const [images, setImages] = React.useState([]);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const [currentImage, setCurrentImage] = React.useState(null);
    const cIInput = React.useRef();

    React.useEffect(() => {
        formik.resetForm({
            values: {
                degreeName: currentItem.certificateName,
                placeOfCertificate: currentItem.issuingInstitution,
                degreeDate: currentItem.issuingDate,
                expriredDate: currentItem.expirationDate ? currentItem.expirationDate : ""
            }
        })
        setImages(Array.from(currentItem.medias))
    }, [currentItem])
    const validate = values => {
        const errors = {};
        if (!values.degreeName) {
            errors.degreeName = "Bắt buộc"
        } else if (values.degreeName.length > 150) {
            errors.degreeName = "Phải dưới 150 ký tự"
        }
        if (!values.placeOfCertificate) {
            errors.placeOfCertificate = "Bắt buộc"
        } else if (values.placeOfCertificate.length > 150) {
            errors.placeOfCertificate = "Phải dưới 150 ký tự"
        }
        if (!values.degreeDate) {
            errors.degreeDate = "Bắt buộc"
        }
        if (!images || images.length === 0) {
            errors.images = "Bắt buộc"
        }
        if (values.degreeDate >= values.expriredDate) {
            errors.expirationDate = "Ngày hết lớn hơn ngày cấp"
        }
        const existCertificate = certificate.find((e) => {
            return e.certificateName.toLowerCase() === values.degreeName.toLowerCase().trim()
                && e.certificateName.toLowerCase() !== currentItem.certificateName.toLowerCase().trim()
        }
        );
        if (existCertificate) {
            errors.degreeName = "Bạn đã thêm chứng chỉ này rồi"
        }
        return errors;
    };
    const formik = useFormik({
        initialValues: {
            degreeName: currentItem.certificateName || "",
            placeOfCertificate: currentItem.issuingInstitution || "",
            degreeDate: currentItem.issuingDate || "",
            expriredDate: currentItem.expirationDate || ""
        },
        validate,
        onSubmit: async (values) => {
            const dataTransfer = new DataTransfer();

            images.forEach(file => {
                dataTransfer.items.add(file);
            });
            const filterCer = certificate.filter((c, i) => i !== index);
            setCertificate([...filterCer, {
                certificateName: values.degreeName.trim(),
                issuingInstitution: values.placeOfCertificate.trim(),
                issuingDate: values.degreeDate,
                expirationDate: values.expriredDate === "" ? null : values.expriredDate,
                medias: dataTransfer.files
            }])
            setOpen(false);
            formik.resetForm();
            setImages([])
        }
    });

    const getMinDate = () => {
        const today = new Date();
        const fifteenYearsAgo = new Date(today);
        fifteenYearsAgo.setFullYear(today.getFullYear() - 70, 0, 1);
        const year = fifteenYearsAgo.getFullYear();
        const month = String(fifteenYearsAgo.getMonth() + 1).padStart(2, '0');
        const day = String(fifteenYearsAgo.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    const getMaxDate = () => {
        const today = new Date();
        const lastYear = new Date(today);
        lastYear.setFullYear(today.getFullYear() + 70);
        const year = lastYear.getFullYear();
        const month = String(lastYear.getMonth() + 1).padStart(2, '0');
        const day = String(lastYear.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
    return (
        <div>
            <ListItemButton >
                <ListItemIcon onClick={handleOpen}>
                    <SchoolIcon />
                </ListItemIcon>
                <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", flexGrow: 1 }} gap={2}>
                    <Typography onClick={handleOpen}>{currentItem.certificateName}</Typography>
                    <ConfirmDeleteDialog certificate={certificate} setCertificate={setCertificate} index={index} />
                </Stack>
            </ListItemButton>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style}>
                    <Typography id="modal-modal-title" variant="h5" component="h2">
                        Thêm bằng cấp hoặc chứng chỉ
                    </Typography>
                    <form onSubmit={formik.handleSubmit}>
                        <Grid container px="50px" py="50px" columnSpacing={2} rowSpacing={3}>
                            <Grid item xs={3} textAlign="right">Tên giấy tờ</Grid>
                            <Grid item xs={9}>
                                <TextField size='small' fullWidth value={formik.values.degreeName}
                                    name='degreeName'
                                    onChange={formik.handleChange} />
                                {
                                    formik.errors.degreeName && (
                                        <FormHelperText error>
                                            {formik.errors.degreeName}
                                        </FormHelperText>
                                    )
                                }
                            </Grid>
                            <Grid item xs={3} textAlign="right">Nơi cấp</Grid>
                            <Grid item xs={9}>
                                <TextField size='small' fullWidth value={formik.values.placeOfCertificate}
                                    name='placeOfCertificate'
                                    onChange={formik.handleChange} />
                                {
                                    formik.errors.placeOfCertificate && (
                                        <FormHelperText error>
                                            {formik.errors.placeOfCertificate}
                                        </FormHelperText>
                                    )
                                }
                            </Grid>
                            <Grid item xs={3} textAlign="right">Ngày cấp</Grid>
                            <Grid item xs={9}>
                                <TextField size='small' type='date' value={formik.values.degreeDate}
                                    name='degreeDate'
                                    onChange={formik.handleChange}
                                    inputProps={{
                                        min: getMinDate(),
                                        max: new Date().toISOString().split('T')[0]
                                    }} />
                                {
                                    formik.errors.degreeDate && (
                                        <FormHelperText error>
                                            {formik.errors.degreeDate}
                                        </FormHelperText>
                                    )
                                }
                            </Grid>
                            <Grid item xs={3} textAlign="right">Ngày hết hạn (không bắt buộc)</Grid>
                            <Grid item xs={9}>
                                <TextField size='small' type='date' value={formik.values.expriredDate}
                                    name='expriredDate'
                                    onChange={formik.handleChange}
                                    inputProps={{
                                        min: formik.values.degreeDate,
                                        max: getMaxDate()
                                    }} />
                                {
                                    formik.errors.expirationDate && (
                                        <FormHelperText error>
                                            {formik.errors.expirationDate}
                                        </FormHelperText>
                                    )
                                }
                            </Grid>
                            <Grid item xs={3} textAlign="right">Tải ảnh</Grid>
                            <Grid item xs={9}>
                                <Box>
                                    <TextField size='small' type='file' inputProps={{
                                        multiple: true,
                                        accept: "image/png, image/jpeg"
                                    }}
                                        onChange={(e) => {
                                            if (e.target.files.length > 5) {
                                                enqueueSnackbar("Chỉ chọn tối đa 5 ảnh", { variant: "error" });
                                                e.target.value = "";
                                            } else {
                                                setImages(Array.from(e.target.files))
                                            }
                                        }}
                                        key={images?.length}
                                        ref={cIInput}
                                    />
                                </Box>
                                <Typography variant='caption'>(Tối đa 5 ảnh)</Typography>
                                {
                                    images.length === 0 && (
                                        <FormHelperText error>
                                            Bắt buộc
                                        </FormHelperText>
                                    )
                                }
                                <Stack direction="row" gap={2} flexWrap="wrap">
                                    {
                                        images && images.map((image, index) => {
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
                                                            const fArray = images.filter((img, i) => {
                                                                return i !== index;
                                                            })
                                                            setImages(fArray)
                                                        }} />
                                                    </Box>
                                                </Box>
                                            )
                                        })
                                    }
                                </Stack>
                            </Grid>
                        </Grid>
                        <Box sx={{ display: "flex", justifyContent: "end", gap: 2 }}>
                            <Button onClick={handleClose}>Huỷ</Button>
                            <Button variant='contained' type='submit'>Lưu</Button>
                        </Box>
                    </form>

                </Box>
            </Modal>
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
                                src={URL.createObjectURL(images[currentImage])}
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
                                onClick={() => setCurrentImage(currentImage === images.length - 1 ? currentImage : currentImage + 1)}
                            >
                                <ArrowForwardIosIcon />
                            </IconButton>
                        </Box>
                    </Modal>
                )
            }
        </div>
    );
}