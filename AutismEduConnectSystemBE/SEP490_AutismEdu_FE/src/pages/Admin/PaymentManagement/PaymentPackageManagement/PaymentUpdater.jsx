import CircleIcon from '@mui/icons-material/Circle';
import { FormHelperText, Stack, TextField } from '@mui/material';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Modal from '@mui/material/Modal';
import Typography from '@mui/material/Typography';
import { useFormik } from 'formik';
import { enqueueSnackbar } from 'notistack';
import PropTypes from 'prop-types';
import * as React from 'react';
import { NumericFormat } from 'react-number-format';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 700,
    bgcolor: 'background.paper',
    boxShadow: 24,
    p: 4
};

const NumericFormatCustom = React.forwardRef(
    function NumericFormatCustom(props, ref) {
        const { onChange, ...other } = props;

        return (
            <NumericFormat
                {...other}
                getInputRef={ref}
                onValueChange={(values) => {
                    onChange({
                        target: {
                            name: props.name,
                            value: values.value,
                        },
                    });
                }}
                isAllowed={(values) => {
                    const { floatValue } = values;
                    return floatValue === undefined || floatValue >= 0;
                }}
                thousandSeparator="."
                decimalSeparator=","
                valueIsNumericString
            />
        );
    },
);

NumericFormatCustom.propTypes = {
    name: PropTypes.string.isRequired,
    onChange: PropTypes.func.isRequired,
};
export default function PaymentUpdater({ paymentPackage, setStatus, status, setPaymentPackages, paymentPackages }) {
    const [open, setOpen] = React.useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const [value, setValue] = React.useState({
        price: ""
    });
    const [loading, setLoading] = React.useState(false);
    const [change, setChange] = React.useState(false);
    const validate = values => {
        const errors = {}
        if (!values.title) {
            errors.title = "Bắt buộc";
        } else if (values.title.length > 100) {
            errors.title = "Tên chỉ chứa tối đa 100 kí tự";
        }
        if (values.description.length > 500) {
            errors.description = "Phải dưới 500 ký tự"
        }
        if (!values.duration) {
            errors.duration = "Bắt buộc";
        }
        if (!values.price) {
            errors.price = "Bắt buộc";
        } else if (Number(values.price) < 10000) {
            errors.price = "Giá tối thiểu là 10.000 vnđ"
        } else if (Number(values.price) > 1000000000) {
            errors.price = "Giá tối đa là 1.000.000.000 vnđ"
        }
        return errors
    }
    const formik = useFormik({
        initialValues: {
            title: '',
            duration: '',
            description: '',
            price: '',
            isHide: true,
            originalId: 0
        }, validate,
        onSubmit: async (values) => {
            try {
                setLoading(true);
                await services.PackagePaymentAPI.createPaymentPackage(values, (res) => {
                    if (status === "Show") {
                        const filterArr = paymentPackages.filter((r) => {
                            return r.id !== res.result.original.id;
                        })
                        setPaymentPackages(filterArr);
                    } else {
                        const filterArr = paymentPackages.map((r) => {
                            if (r.id === res.result.original.id) {
                                return res.result
                            } else
                                return r
                        })
                        setPaymentPackages(filterArr);
                    }
                    enqueueSnackbar("Cập nhật gói thanh toán thành công", { variant: "success" });
                    handleClose();
                }, (error) => {
                    enqueueSnackbar(error.error[0], { variant: "error" })
                })
                setLoading(false)
            } catch (error) {
                enqueueSnackbar("Cập nhật gói thanh toán thất bại", { variant: "error" })
                setLoading(false);
            }
        }
    })

    React.useEffect(() => {
        if (paymentPackage) {
            formik.resetForm({
                values: {
                    title: paymentPackage.title || '',
                    duration: paymentPackage.duration || '',
                    description: paymentPackage.description || '',
                    price: paymentPackage.price || '',
                    originalId: paymentPackage.id || 0,
                    isHide: true
                }
            });
        }
    }, [paymentPackage]);

    React.useEffect(() => {
        if (paymentPackage) {
            if (formik.values.title.trim() !== paymentPackage.title) {
                setChange(false);
                return;
            }
            if (formik.values.duration !== paymentPackage.duration) {
                setChange(false);
                return;
            }
            if (formik.values.description.trim() !== paymentPackage.description) {
                setChange(false);
                return;
            }
            if (formik.values.price !== paymentPackage.price) {
                setChange(false);
                return;
            }
            setChange(true);
        }
    }, [formik])
    const handleChange = (event) => {
        if (event.target.value[0] === "-") {
            formik.setFieldValue(event.target.name, "0")
        } else {
            formik.setFieldValue(event.target.name, event.target.value)
        }
        setValue({
            ...value,
            [event.target.name]: event.target.value
        });
    };

    return (
        <>
            <Button onClick={handleOpen} variant='outlined' sx={{ ml: 3 }}>Chi tiết</Button>
            <Modal
                open={open}
                onClose={handleClose}
            >
                <Box sx={style}>
                    <Typography variant="h5" component="h2">
                        Cập nhật gói thanh toán
                    </Typography>
                    <Stack direction='row' alignItems="center" mt={2}>
                        <CircleIcon sx={{ color: paymentPackage?.isHide ? "red" : "green" }} />
                        <Typography sx={{ color: paymentPackage?.isHide ? "red" : "green" }}>
                            {paymentPackage?.isHide ? "Đang ẩn" : "Đang công khai"}
                        </Typography>
                    </Stack>
                    {
                        paymentPackage?.isHide === false && (
                            <Typography sx={{ color: "red", fontSize: "12px" }}>Khi cập nhật gói thanh toán sẽ về chế độ ẩn</Typography>
                        )
                    }
                    <form onSubmit={formik.handleSubmit}>
                        <Typography mt={3}>Tiêu đề</Typography>
                        <TextField
                            fullWidth
                            name='title'
                            value={formik.values.title}
                            onChange={formik.handleChange}
                        />
                        {
                            formik.errors.title && (
                                <FormHelperText error>
                                    {formik.errors.title}
                                </FormHelperText>
                            )
                        }
                        <Typography mt={2}>Mô tả</Typography>
                        <TextField
                            multiline
                            rows={5}
                            fullWidth
                            name='description'
                            value={formik.values.description}
                            onChange={formik.handleChange}
                        />
                        <Typography sx={{ fontSize: '12px', textAlign: "right" }}>
                            {formik.values.description.length} / 500
                        </Typography>
                        {
                            formik.errors.description && (
                                <FormHelperText error>
                                    {formik.errors.description}
                                </FormHelperText>
                            )
                        }
                        <Stack direction='row' mt={2}>
                            <Box sx={{ width: "50%" }}>
                                <Typography>Khoảng thời gian (tháng): </Typography>
                                <TextField
                                    name='duration'
                                    onChange={formik.handleChange}
                                    value={formik.values.duration}
                                    type='Number'
                                    inputProps={{
                                        min: 1,
                                        max: 1200
                                    }}
                                />
                            </Box>
                            <Box sx={{ width: "50%" }}>
                                <Typography>Giá (VNĐ)</Typography>
                                <TextField
                                    variant="outlined"
                                    name="price"
                                    value={formik.values.price}
                                    onChange={handleChange}
                                    InputProps={{
                                        inputComponent: NumericFormatCustom,
                                    }}
                                />
                            </Box>
                        </Stack>
                        <Stack direction='row' justifyContent="space-between">
                            <Box sx={{ width: "50%" }}>
                                {
                                    formik.errors.duration && (
                                        <FormHelperText error>
                                            {formik.errors.duration}
                                        </FormHelperText>
                                    )
                                }
                            </Box>
                            <Box sx={{ width: "50%" }}>
                                {
                                    formik.errors.price && (
                                        <FormHelperText error>
                                            {formik.errors.price}
                                        </FormHelperText>
                                    )
                                }
                            </Box>
                        </Stack>
                        <Box>
                            <Button variant='contained' type='submit' sx={{ mt: 2 }} disabled={change}>Cập nhật</Button>
                        </Box>
                    </form>
                    <LoadingComponent open={loading} />
                </Box>
            </Modal>
        </>
    );
}